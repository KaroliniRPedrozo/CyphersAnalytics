import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5185/api/v1',
});

// Adiciona o token JWT em todas as requisições automaticamente
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export default api;