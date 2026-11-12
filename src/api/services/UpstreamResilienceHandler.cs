using System.Net;

namespace api.services
{
    /// <summary>
    /// Limitador de janela deslizante para chamadas de SAIDA (compartilhado por todo o processo).
    /// Garante que nunca excedemos o limite da chave, mesmo com varios usuarios ao mesmo tempo.
    /// </summary>
    public sealed class UpstreamThrottle
    {
        private readonly int _max;
        private readonly TimeSpan _window;
        private readonly Queue<DateTime> _stamps = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private DateTime _pausedUntil = DateTime.MinValue;

        public UpstreamThrottle(int maxRequests, TimeSpan window)
        {
            _max = Math.Max(1, maxRequests);
            _window = window;
        }

        /// <summary>Segundos restantes de pausa imposta por um 429 anterior (0 se nao ha pausa).</summary>
        public TimeSpan PausedFor()
        {
            var left = _pausedUntil - DateTime.UtcNow;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        public void PauseFor(TimeSpan duration)
        {
            var until = DateTime.UtcNow + duration;
            if (until > _pausedUntil) _pausedUntil = until;
        }

        public async Task WaitAsync(CancellationToken ct)
        {
            while (true)
            {
                TimeSpan wait;
                await _lock.WaitAsync(ct);
                try
                {
                    var now = DateTime.UtcNow;
                    while (_stamps.Count > 0 && now - _stamps.Peek() >= _window) _stamps.Dequeue();

                    if (_stamps.Count < _max)
                    {
                        _stamps.Enqueue(now);
                        return;
                    }
                    wait = _window - (now - _stamps.Peek());
                }
                finally { _lock.Release(); }

                await Task.Delay(wait > TimeSpan.Zero ? wait : TimeSpan.FromMilliseconds(10), ct);
            }
        }
    }

    /// <summary>
    /// Handler HTTP de saida: respeita o limite local, trata 429 (Retry-After) e faz retry com
    /// backoff em 5xx/timeouts. Se a espera exigida for longa demais, devolve 429 na hora para
    /// o controller responder rapido (em vez de segurar a requisicao do usuario).
    /// </summary>
    public sealed class UpstreamResilienceHandler : DelegatingHandler
    {
        private const int MaxAttempts = 3;
        private static readonly TimeSpan MaxInlineWait = TimeSpan.FromSeconds(8);

        private readonly UpstreamThrottle _throttle;
        private readonly ILogger<UpstreamResilienceHandler> _logger;

        public UpstreamResilienceHandler(UpstreamThrottle throttle, ILogger<UpstreamResilienceHandler> logger)
        {
            _throttle = throttle;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // Todas as chamadas desta aplicacao sao GET sem corpo, portanto podem ser reenviadas com seguranca.
            if (request.Method != HttpMethod.Get)
                return await base.SendAsync(request, ct);

            for (var attempt = 1; ; attempt++)
            {
                var pausa = _throttle.PausedFor();
                if (pausa > MaxInlineWait)
                    return RespostaLimitada(pausa);
                if (pausa > TimeSpan.Zero)
                    await Task.Delay(pausa, ct);

                await _throttle.WaitAsync(ct);

                HttpResponseMessage? response = null;
                try
                {
                    response = await base.SendAsync(Clonar(request), ct);
                }
                catch (HttpRequestException) when (attempt < MaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
                    continue;
                }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested && attempt < MaxAttempts)
                {
                    // timeout do HttpClient
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
                    continue;
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var espera = LerRetryAfter(response) ?? TimeSpan.FromSeconds(5 * attempt);
                    _throttle.PauseFor(espera);
                    _logger.LogWarning("Upstream devolveu 429. Retry-After={Espera}s (tentativa {Attempt}/{Max})",
                        espera.TotalSeconds, attempt, MaxAttempts);

                    if (attempt >= MaxAttempts || espera > MaxInlineWait) return response;
                    response.Dispose();
                    await Task.Delay(espera, ct);
                    continue;
                }

                if ((int)response.StatusCode >= 500 && attempt < MaxAttempts)
                {
                    _logger.LogWarning("Upstream devolveu {Status} (tentativa {Attempt}/{Max})",
                        (int)response.StatusCode, attempt, MaxAttempts);
                    response.Dispose();
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
                    continue;
                }

                return response;
            }
        }

        private static HttpResponseMessage RespostaLimitada(TimeSpan retryAfter)
        {
            var r = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(retryAfter);
            return r;
        }

        private static TimeSpan? LerRetryAfter(HttpResponseMessage response)
        {
            var ra = response.Headers.RetryAfter;
            if (ra?.Delta is { } delta) return delta;
            if (ra?.Date is { } date)
            {
                var d = date - DateTimeOffset.UtcNow;
                return d > TimeSpan.Zero ? d : TimeSpan.FromSeconds(1);
            }
            return null;
        }

        private static HttpRequestMessage Clonar(HttpRequestMessage original)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri) { Version = original.Version };
            foreach (var h in original.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
            return clone;
        }
    }
}
