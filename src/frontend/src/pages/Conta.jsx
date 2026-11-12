import { useEffect, useState, useCallback } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import api, { mensagemDeErro } from '../services/api';

const MENSAGENS_RSO = {
  ok: ['sucesso', 'Conta Riot vinculada com sucesso!'],
  negado: ['erro', 'A autorização na Riot foi cancelada.'],
  conta_ja_vinculada: ['erro', 'Esta conta Riot já está vinculada a outro usuário.'],
  indisponivel: ['erro', 'A vinculação com a Riot ainda não está disponível.'],
  erro: ['erro', 'Não foi possível concluir a vinculação. Tente novamente.'],
};

export default function Conta() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [conta, setConta] = useState(null);
  const [aviso, setAviso] = useState(() => MENSAGENS_RSO[params.get('rso')] || null); // [tipo, texto]
  const [senha, setSenha] = useState('');
  const [confirmandoExclusao, setConfirmandoExclusao] = useState(false);
  const [ocupado, setOcupado] = useState(false);

  const carregar = useCallback(() =>
    api.get('/Auth/conta')
      .then(({ data }) => setConta(data))
      .catch((err) => setAviso(['erro', mensagemDeErro(err, 'Não foi possível carregar sua conta.')])),
  []);

  useEffect(() => { carregar(); }, [carregar]);

  async function executar(fn, sucesso) {
    setOcupado(true); setAviso(null);
    try { await fn(); if (sucesso) setAviso(['sucesso', sucesso]); await carregar(); }
    catch (err) { setAviso(['erro', mensagemDeErro(err)]); }
    finally { setOcupado(false); }
  }

  const autorizar = () => executar(async () => {
    await api.post('/Auth/consentimento', { aceito: true });
    sessionStorage.clear();
  }, 'Autorização registrada. Você já pode ver seus dados.');

  const revogar = () => {
    if (!window.confirm('Revogar a autorização apaga o seu histórico de partidas guardado. Continuar?')) return;
    executar(async () => { await api.delete('/Auth/consentimento'); sessionStorage.clear(); },
      'Autorização revogada e dados de jogo removidos.');
  };

  const vincularRiot = () => executar(async () => {
    const { data } = await api.post('/rso/iniciar');
    window.location.assign(data.url);
  });

  async function baixarDados() {
    setOcupado(true); setAviso(null);
    try {
      const { data } = await api.get('/Auth/meus-dados');
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = 'meus-dados-cyphers-analytics.json'; a.click();
      URL.revokeObjectURL(url);
    } catch (err) { setAviso(['erro', mensagemDeErro(err)]); }
    finally { setOcupado(false); }
  }

  async function excluirConta(e) {
    e.preventDefault();
    setOcupado(true); setAviso(null);
    try {
      await api.delete('/Auth/conta', { data: { senha } });
      localStorage.clear(); sessionStorage.clear();
      navigate('/');
    } catch (err) { setAviso(['erro', mensagemDeErro(err)]); setOcupado(false); }
  }

  return (
    <main style={s.page}>
      <div style={s.card}>
        <Link to="/dashboard" style={s.voltar}>← Voltar ao painel</Link>
        <h1 style={s.h1}>Minha conta</h1>

        {aviso && <div style={aviso[0] === 'sucesso' ? s.ok : s.erro}>{aviso[1]}</div>}
        {!conta && !aviso && <p style={s.mudo}>Carregando...</p>}

        {conta && (<>
          <p style={s.mudo}>{conta.email} · {conta.gameName}#{conta.tagLine}</p>

          <section style={s.secao}>
            <h2 style={s.h2}>Autorização de uso dos dados</h2>
            {conta.consentimento ? (<>
              <p style={s.p}>Você autorizou o uso dos seus dados de jogo em {new Date(conta.consentimentoEm).toLocaleDateString('pt-BR')}. Só você vê essas informações.</p>
              <button style={s.btnSec} onClick={revogar} disabled={ocupado}>Revogar autorização</button>
            </>) : (<>
              <p style={s.p}>
                Para mostrar suas estatísticas precisamos da sua autorização para consultar e guardar os dados das suas partidas.
                Veja a <Link to="/privacidade" style={s.link}>Política de Privacidade</Link> e os <Link to="/termos" style={s.link}>Termos de Uso</Link>.
              </p>
              <button style={s.btn} onClick={autorizar} disabled={ocupado}>Autorizar uso dos meus dados</button>
            </>)}
          </section>

          <section style={s.secao}>
            <h2 style={s.h2}>Conta Riot (Riot Sign On)</h2>
            {conta.contaRiotVinculada ? (
              <p style={s.p}>Conta Riot verificada em {new Date(conta.rsoVerificadoEm).toLocaleDateString('pt-BR')}.</p>
            ) : conta.rsoHabilitado ? (<>
              <p style={s.p}>Entre com a sua conta Riot para confirmar que o Riot ID é seu. Você será levado ao site da Riot para autorizar; nunca vemos a sua senha da Riot.</p>
              <button style={s.btn} onClick={vincularRiot} disabled={ocupado}>Vincular conta Riot</button>
            </>) : (
              <p style={s.p}>A verificação por login Riot estará disponível assim que a Riot liberar a integração oficial.</p>
            )}
          </section>

          <section style={s.secao}>
            <h2 style={s.h2}>Seus dados</h2>
            <p style={s.p}>Baixe uma cópia de tudo o que guardamos sobre você.</p>
            <button style={s.btnSec} onClick={baixarDados} disabled={ocupado}>Baixar meus dados (JSON)</button>
          </section>

          <section style={s.secao}>
            <h2 style={{ ...s.h2, color: '#ff4655' }}>Excluir conta</h2>
            {!confirmandoExclusao ? (
              <button style={s.btnPerigo} onClick={() => setConfirmandoExclusao(true)}>Excluir minha conta</button>
            ) : (
              <form onSubmit={excluirConta}>
                <p style={s.p}>Isto apaga definitivamente sua conta e todo o histórico. Confirme com a sua senha.</p>
                <input style={s.input} type="password" placeholder="Senha" value={senha}
                  onChange={e => setSenha(e.target.value)} required autoComplete="current-password" />
                <button style={s.btnPerigo} type="submit" disabled={ocupado || !senha}>Excluir definitivamente</button>
              </form>
            )}
          </section>
        </>)}
      </div>
    </main>
  );
}

const s = {
  page: { minHeight: '100vh', background: '#0b0f14', padding: '2rem 1rem 5rem', display: 'flex', justifyContent: 'center', fontFamily: "'Segoe UI',sans-serif" },
  card: { width: '100%', maxWidth: 640, color: '#d1d5db' },
  voltar: { color: '#ff4655', textDecoration: 'none', fontWeight: 700, fontSize: '0.9rem' },
  h1: { color: '#fff', fontSize: '1.8rem', fontWeight: 900, margin: '1rem 0 0.25rem' },
  h2: { color: '#fff', fontSize: '1.05rem', marginBottom: '0.5rem' },
  p: { fontSize: '0.92rem', lineHeight: 1.6, marginBottom: '0.75rem' },
  mudo: { color: '#6b7280', fontSize: '0.85rem', marginBottom: '1rem' },
  link: { color: '#ff4655' },
  secao: { background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.08)', borderRadius: 6, padding: '1.1rem 1.25rem', marginTop: '1rem' },
  input: { display: 'block', width: '100%', boxSizing: 'border-box', padding: '0.7rem 0.9rem', marginBottom: '0.75rem', borderRadius: 4, border: '1px solid rgba(255,255,255,0.15)', background: 'rgba(255,255,255,0.05)', color: '#fff' },
  btn: { padding: '0.7rem 1.1rem', background: '#ff4655', color: '#fff', border: 'none', borderRadius: 4, fontWeight: 800, cursor: 'pointer' },
  btnSec: { padding: '0.7rem 1.1rem', background: 'transparent', color: '#d1d5db', border: '1px solid rgba(255,255,255,0.25)', borderRadius: 4, fontWeight: 700, cursor: 'pointer' },
  btnPerigo: { padding: '0.7rem 1.1rem', background: 'transparent', color: '#ff4655', border: '1px solid #ff4655', borderRadius: 4, fontWeight: 800, cursor: 'pointer' },
  ok: { background: 'rgba(63,185,80,0.1)', border: '1px solid rgba(63,185,80,0.3)', color: '#3fb950', padding: '0.75rem 0.9rem', borderRadius: 4, marginTop: '1rem', fontSize: '0.9rem' },
  erro: { background: 'rgba(255,70,85,0.1)', border: '1px solid rgba(255,70,85,0.3)', color: '#ff4655', padding: '0.75rem 0.9rem', borderRadius: 4, marginTop: '1rem', fontSize: '0.9rem' },
};
