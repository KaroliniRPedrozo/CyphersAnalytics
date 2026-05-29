import { useLocation, useNavigate } from 'react-router-dom';
import { getAgenteIcone, getMapaImg } from '../services/assets';

const styles = {
  page: { 
    boxSizing: 'border-box',
    background: 'radial-gradient(circle at top right, #161b22 0%, #0a0d14 100%)', 
    height: '100vh', 
    color: '#ece8e1', 
    padding: '3rem 5% 5rem 5%', 
    fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
    overflowY: 'auto'
  },
  header: { display: 'flex', alignItems: 'center', gap: '2rem', marginBottom: '3rem', borderBottom: '1px solid rgba(255, 255, 255, 0.05)', paddingBottom: '1.5rem' },
  btnVoltar: { background: 'rgba(236, 232, 225, 0.05)', border: '1px solid rgba(236, 232, 225, 0.2)', color: '#ece8e1', padding: '0.6rem 1.2rem', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold', letterSpacing: '1px', transition: 'all 0.3s ease' },
  titulo: { fontSize: '2rem', letterSpacing: '4px', fontWeight: '800', margin: 0, textTransform: 'uppercase' },
  
  overviewGrid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.5rem', marginBottom: '2.5rem' },
  cardGlass: { background: 'rgba(22, 27, 34, 0.6)', backdropFilter: 'blur(10px)', WebkitBackdropFilter: 'blur(10px)', padding: '1.5rem', borderRadius: '12px', border: '1px solid rgba(255, 255, 255, 0.05)', boxShadow: '0 10px 30px rgba(0,0,0,0.3)', display: 'flex', flexDirection: 'column', gap: '0.25rem' },
  cardLabel: { fontSize: '0.75rem', color: '#978b8b', letterSpacing: '1.5px', textTransform: 'uppercase', fontWeight: '700' },
  cardValue: { fontSize: '2.2rem', fontWeight: '900', color: '#fff' },
  
  listaContainer: { background: 'rgba(22, 27, 34, 0.4)', borderRadius: '12px', border: '1px solid rgba(255, 255, 255, 0.05)', overflow: 'hidden', display: 'flex', flexDirection: 'column' },
  listHeader: { padding: '1.25rem 1.5rem', borderBottom: '1px solid rgba(255, 255, 255, 0.05)', color: '#ffffff', fontSize: '0.9rem', fontWeight: '800', letterSpacing: '2px' },
  
  pRow: { display: 'flex', alignItems: 'stretch', height: '48px', background: 'rgba(255,255,255,0.01)', borderBottom: '1px solid rgba(255,255,255,0.03)', borderLeft: '3px solid transparent' },
  pAgIcon: { width: 48, height: '100%', objectFit: 'cover', flexShrink: 0 },
  pAgVazio: { width: 48, height: '100%', background: '#0f1520', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#374151', fontSize: '1rem', fontWeight: 900, flexShrink: 0 },
  
  pMapaContainer: { flex: 1, position: 'relative', display: 'flex', alignItems: 'center', paddingLeft: '1.25rem', overflow: 'hidden' },
  pMapaNome: { fontSize: '1.1rem', fontWeight: 900, letterSpacing: 1.5, color: '#ece8e1', position: 'relative', zIndex: 2 },
  // Brightened maps: opacity increased from 0.4 to 0.75
  pMapaImg: { position: 'absolute', right: 0, top: 0, height: '100%', width: '130px', objectFit: 'cover', maskImage: 'linear-gradient(to right, transparent 0%, black 80%)', WebkitMaskImage: 'linear-gradient(to right, transparent 0%, black 80%)', opacity: 0.75, zIndex: 1 },
  
  // Largura fixa para garantir que não esmaga os outros elementos
  pModo: { width: '150px', display: 'flex', alignItems: 'center', color: '#8b978f', fontSize: '0.85rem', fontWeight: '700', letterSpacing: '1px', textTransform: 'uppercase', paddingLeft: '1rem', flexShrink: 0 },
  
  // O Wrapper invisível que ocupa o espaço livre central
  pTagWrapper: { flex: 1, display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' },
  
  // A Tag agora tem altura a 100% para manter a barra sólida
  pTag: { 
    width: '110px', 
    height: '100%',
    display: 'flex', 
    alignItems: 'center', 
    justifyContent: 'center', 
    flexShrink: 0 
  },
  
  pStats: { 
    display: 'grid', 
    gridTemplateColumns: '90px 90px 90px', 
    justifyContent: 'end', 
    alignItems: 'center', 
    gap: '1rem', 
    paddingRight: '2rem', 
    color: '#ece8e1', 
    fontSize: '0.9rem', 
    fontWeight: 800, 
    whiteSpace: 'nowrap'
  }
};

export default function DetalhesPartidas() {
  const { state } = useLocation();
  const navigate = useNavigate();

  if (!state || !state.historico) {
    return (
      <div style={{...styles.page, display: 'flex', justifyContent: 'center', alignItems: 'center', flexDirection: 'column', gap: '2rem'}}>
        <h2 style={styles.titulo}>Nenhum histórico encontrado</h2>
        <button style={styles.btnVoltar} onClick={() => navigate('/dashboard')}>VOLTAR PARA A DASHBOARD</button>
      </div>
    );
  }

  const { historico, partidasFiltradas } = state;
  const listaExibir = partidasFiltradas || historico;

  // KPIs calculados com base no filtro ativo trazido da Dashboard
  const totalPartidas = listaExibir.length;
  const vitorias = listaExibir.filter(p => p.resultado === 'Vitoria').length;
  const derrotas = totalPartidas - vitorias;
  const winRate = totalPartidas > 0 ? Math.round((vitorias / totalPartidas) * 100) : 0;
  
  const avgKills = totalPartidas > 0 ? (listaExibir.reduce((acc, p) => acc + p.kills, 0) / totalPartidas).toFixed(1) : '0.0';
  const avgACS = totalPartidas > 0 ? Math.round(listaExibir.reduce((acc, p) => acc + Math.round(p.score / 20), 0) / totalPartidas) : 0;

  return (
    <div style={styles.page}>
      <header style={styles.header}>
        <button 
          onClick={() => navigate(-1)} 
          style={styles.btnVoltar}
          onMouseOver={(e) => { e.currentTarget.style.background = 'rgba(236, 232, 225, 0.1)'; e.currentTarget.style.transform = 'translateX(-5px)'; }}
          onMouseOut={(e) => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.transform = 'translateX(0)'; }}
        >
          ← VOLTAR
        </button>
        <h1 style={styles.titulo}>HISTÓRICO COMPLETO DE CONFRONTOS</h1>
      </header>

      {/* PAINEL DE SÍNTESE ANALÍTICA */}
      <div style={styles.overviewGrid}>
        <div style={styles.cardGlass}>
          <span style={styles.cardLabel}>Partidas Listadas</span>
          <span style={styles.cardValue}>{totalPartidas}</span>
        </div>
        <div style={styles.cardGlass}>
          <span style={styles.cardLabel}>Taxa de Vitória</span>
          <span style={{ ...styles.cardValue, color: winRate >= 50 ? '#00ff87' : '#ff4655' }}>{winRate}%</span>
        </div>
        <div style={styles.cardGlass}>
          <span style={styles.cardLabel}>Média de Kills</span>
          <span style={styles.cardValue}>{avgKills}</span>
        </div>
        <div style={styles.cardGlass}>
          <span style={styles.cardLabel}>Média de Combat Score (ACS)</span>
          <span style={{ ...styles.cardValue, color: '#ffffff' }}>{avgACS}</span>
        </div>
      </div>

      {/* LISTAGEM BLOCADA ESTILO FIGMA */}
      <div style={styles.listaContainer}>
        <div style={styles.listHeader}>REGISTROS RECENTES ({vitorias} VITÓRIAS — {derrotas} DERROTAS)</div>
        <div style={{ display: 'flex', flexDirection: 'column' }}>
          {listaExibir.map((p, i) => {
            const vitoria = p.resultado === 'Vitoria';
            const mapaImg = getMapaImg(p.mapa);
            const agIcon  = getAgenteIcone(p.agente);
            return (
              <div key={i} style={{ ...styles.pRow, borderLeftColor: vitoria ? '#00ff87' : '#ff4655' }}>
                {/* Avatar do Agente */}
                {agIcon
                  ? <img src={agIcon} alt={p.agente} style={styles.pAgIcon} loading="lazy" />
                  : <div style={styles.pAgVazio}>{p.agente?.[0]}</div>
                }
                
                {/* Mapa Componentizado */}
                <div style={styles.pMapaContainer}>
                  <span style={styles.pMapaNome}>{p.mapa?.toUpperCase()}</span>
                  {mapaImg && <img src={mapaImg} alt={p.mapa} style={styles.pMapaImg} loading="lazy" />}
                </div>

                {/* Modo de Jogo com largura fixa */}
                <div style={styles.pModo}>
                  {p.modo || 'Custom'}
                </div>

                {/* Contentor Invisível para Centralizar a Tag no meio do espaço livre */}
                <div style={styles.pTagWrapper}>
                  {/* Fading Status Tags: Solid on right, transparent on left (replicating map effect) */}
                  <div style={{ 
                    ...styles.pTag, 
                    background: vitoria 
                      ? 'linear-gradient(to right, transparent, #127e5a)' 
                      : 'linear-gradient(to right, transparent, #ad2f41)' 
                  }}>
                    <span style={{ color: vitoria ? '#4ff2a9' : '#ff7a8a', fontWeight: 900, fontSize: '0.85rem', letterSpacing: 1.5 }}>
                      {vitoria ? 'VITÓRIA' : 'DERROTA'}
                    </span>
                  </div>
                </div>

                {/* Estatísticas Finais da Partida */}
                <div style={styles.pStats}>
                  <span style={{ color: '#8b978f', textAlign: 'left' }}>
                    ACS: <strong style={{ color: '#fff' }}>{Math.round(p.score / 20)}</strong>
                  </span>
                  <span style={{ color: '#8b978f', textAlign: 'left' }}>
                    KILLS: <strong style={{ color: '#fff' }}>{p.kills}</strong>
                  </span>
                  {p.assists !== undefined ? (
                    <span style={{ color: '#8b978f', textAlign: 'left' }}>
                      AST: <strong style={{ color: '#fff' }}>{p.assists}</strong>
                    </span>
                  ) : (
                    <span /> /* Placeholder para manter a 3ª coluna alinhada caso não haja AST */
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}