import { Link } from 'react-router-dom';

// Aviso legal exigido pela política de desenvolvedores da Riot Games (deve ficar visível ao jogador).
export const AVISO_RIOT =
  "Cypher's Analytics não é endossado pela Riot Games e não reflete as visões ou opiniões da Riot Games " +
  'ou de qualquer pessoa oficialmente envolvida na produção ou gestão das propriedades da Riot Games. ' +
  'Riot Games e todas as propriedades associadas são marcas comerciais ou marcas registradas da Riot Games, Inc.';

export default function Rodape() {
  return (
    <footer style={s.rodape}>
      <span style={s.texto}>{AVISO_RIOT}</span>
      <nav style={s.links}>
        <Link to="/sobre" style={s.link}>Sobre</Link>
        <Link to="/privacidade" style={s.link}>Privacidade</Link>
        <Link to="/termos" style={s.link}>Termos</Link>
      </nav>
    </footer>
  );
}

const s = {
  rodape: {
    position: 'fixed', left: 0, right: 0, bottom: 0, zIndex: 50,
    display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'center', gap: '0.25rem 1rem',
    padding: '0.4rem 1rem', background: 'rgba(5,8,12,0.92)', borderTop: '1px solid rgba(255,255,255,0.06)',
    fontFamily: "'Segoe UI',sans-serif",
  },
  texto: { color: '#6b7280', fontSize: '0.66rem', lineHeight: 1.4, textAlign: 'center', maxWidth: 980 },
  links: { display: 'flex', gap: '0.9rem' },
  link:  { color: '#9ca3af', fontSize: '0.72rem', textDecoration: 'none', fontWeight: 600 },
};
