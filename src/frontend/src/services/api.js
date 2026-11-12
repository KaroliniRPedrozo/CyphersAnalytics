import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5185/api/v1',
});

// Adiciona o token JWT em todas as requisições automaticamente
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Tratamento central de sessão expirada e de consentimento/vínculo pendente
api.interceptors.response.use(
  (res) => res,
  (err) => {
    const status = err.response?.status;
    const codigo = err.response?.data?.codigo;
    const url = err.config?.url || '';
    const ehAuth = url.includes('/Auth/login') || url.includes('/Auth/registrar');

    if (status === 401 && !ehAuth && localStorage.getItem('token')) {
      localStorage.clear();
      sessionStorage.clear();
      window.location.assign('/');
    } else if (status === 403 && (codigo === 'consentimento_necessario' || codigo === 'rso_necessario')) {
      if (!window.location.pathname.startsWith('/conta')) window.location.assign('/conta');
    }
    return Promise.reject(err);
  }
);

// Mensagem amigável a partir do erro devolvido pela API
export function mensagemDeErro(err, padrao = 'Algo deu errado. Tente novamente.') {
  if (!err.response) return 'Não foi possível conectar ao servidor. Verifique sua conexão.';
  if (err.response.status === 429) {
    const s = err.response.headers?.['retry-after'];
    return err.response.data?.erro || `Muitas tentativas. Aguarde${s ? ` ${s}s` : ' um instante'} e tente de novo.`;
  }
  return err.response.data?.erro || padrao;
}

export default api;
