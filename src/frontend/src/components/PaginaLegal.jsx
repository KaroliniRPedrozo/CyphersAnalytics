import { Link } from 'react-router-dom';

// Layout simples e legível para páginas de texto (Sobre, Privacidade, Termos).
export default function PaginaLegal({ titulo, atualizado, children }) {
  return (
    <main style={s.page}>
      <article style={s.card}>
        <Link to={localStorage.getItem('token') ? '/dashboard' : '/'} style={s.voltar}>← Voltar</Link>
        <h1 style={s.h1}>{titulo}</h1>
        {atualizado && <p style={s.data}>Última atualização: {atualizado}</p>}
        <div style={s.corpo}>{children}</div>
      </article>
    </main>
  );
}

export const H2 = ({ children }) => <h2 style={s.h2}>{children}</h2>;
export const P  = ({ children }) => <p style={s.p}>{children}</p>;
export const UL = ({ children }) => <ul style={s.ul}>{children}</ul>;

const s = {
  page: { minHeight: '100vh', background: '#0b0f14', padding: '2rem 1rem 5rem', display: 'flex', justifyContent: 'center', fontFamily: "'Segoe UI',sans-serif" },
  card: { width: '100%', maxWidth: 820, color: '#d1d5db' },
  voltar: { color: '#ff4655', textDecoration: 'none', fontWeight: 700, fontSize: '0.9rem' },
  h1: { color: '#fff', fontSize: '1.9rem', fontWeight: 900, letterSpacing: 1, margin: '1rem 0 0.25rem' },
  data: { color: '#6b7280', fontSize: '0.8rem', marginBottom: '1.5rem' },
  corpo: { lineHeight: 1.7, fontSize: '0.95rem' },
  h2: { color: '#fff', fontSize: '1.15rem', marginTop: '1.75rem', marginBottom: '0.5rem' },
  p:  { marginBottom: '0.75rem' },
  ul: { margin: '0 0 0.75rem 1.25rem', display: 'grid', gap: '0.35rem' },
};
