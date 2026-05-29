import { useLocation, useNavigate } from 'react-router-dom';
import { getAgenteArtwork, getAgenteHabilidades } from '../services/assets';

const styles = {
  page: { 
    boxSizing: 'border-box',
    background: 'radial-gradient(circle at top right, hsl(215, 21%, 11%) 0%, #0a0d14 100%)', 
    height: '100vh',      // <- Alterado de minHeight para height fixo
    overflowY: 'auto',    // <- Ativa a barra de rolagem vertical exclusiva para esta página
    color: '#ece8e1', 
    padding: '3rem 5% 5rem 5%', // <- Adicionado 5rem de respiro no fundo para os cards não colarem no fim
    fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"
  },
  header: { display: 'flex', alignItems: 'center', gap: '2rem', marginBottom: '3rem', borderBottom: '1px solid rgba(255, 255, 255, 0.05)', paddingBottom: '1.5rem' },
  btnVoltar: { background: 'rgba(236, 232, 225, 0.05)', border: '1px solid rgba(236, 232, 225, 0.2)', color: '#ece8e1', padding: '0.6rem 1.2rem', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold', letterSpacing: '1px', transition: 'all 0.3s ease' },
  titulo: { fontSize: '2rem', letterSpacing: '4px', fontWeight: '800', margin: 0, textTransform: 'uppercase' },
  
  content: { display: 'flex', gap: '4rem', flexWrap: 'wrap', alignItems: 'flex-start' },
  
  // Coluna da Esquerda (Arte)
  colArte: { flex: '1', minWidth: '300px', display: 'flex', flexDirection: 'column', alignItems: 'center', position: 'relative' },
  arteFull: { width: '100%', maxHeight: '700px', objectFit: 'contain', filter: 'drop-shadow(0 20px 30px rgba(0,0,0,0.6))', zIndex: 2 },
  bgGlow: { position: 'absolute', top: '20%', left: '50%', transform: 'translate(-50%, -50%)', width: '300px', height: '300px', background: 'rgba(255, 70, 85, 0.15)', filter: 'blur(80px)', borderRadius: '50%', zIndex: 1 },

  // Coluna da Direita (Status)
  colInfo: { flex: '1.5', minWidth: '400px', display: 'flex', flexDirection: 'column', gap: '2rem' },
  cardGlass: { background: 'rgba(22, 27, 34, 0.6)', backdropFilter: 'blur(10px)', WebkitBackdropFilter: 'blur(10px)', padding: '2rem', borderRadius: '12px', border: '1px solid rgba(255, 255, 255, 0.05)', boxShadow: '0 10px 30px rgba(0,0,0,0.3)' },
  
  secLabel: { color: '#ffffff', fontSize: '0.9rem', fontWeight: '800', letterSpacing: '2px', textTransform: 'uppercase', marginBottom: '1.5rem', display: 'flex', alignItems: 'center', gap: '0.5rem' },
  
  // Grid de Habilidades
  habGrid: { display: 'flex', flexDirection: 'column', gap: '1.5rem' },
  habRow: { display: 'flex', alignItems: 'center', gap: '1.5rem', background: 'rgba(10, 13, 20, 0.4)', padding: '1rem 1.5rem', borderRadius: '8px', border: '1px solid rgba(255,255,255,0.03)' },
  habIconBox: { width: '50px', height: '50px', background: 'rgba(255,255,255,0.05)', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '10px' },
  habIcon: { width: '100%', height: '100%', objectFit: 'contain' },
  habInfo: { flex: 1, display: 'flex', flexDirection: 'column', gap: '0.5rem' },
  habNome: { fontSize: '1.2rem', fontWeight: '700', letterSpacing: '1px' },
  
  // Barras de progresso melhoradas
  barContainer: { display: 'flex', alignItems: 'center', gap: '1rem' },
  barFundo: { flex: 1, height: '12px', background: 'rgba(255,255,255,0.05)', borderRadius: '10px', overflow: 'hidden' },
  barPreen: { height: '100%', borderRadius: '10px', background: 'linear-gradient(90deg, #00ff87, #00e5ff)' },
  barVal: { fontSize: '1rem', fontWeight: '700', minWidth: '40px', textAlign: 'right', color: '#ece8e1' },
  
  // Estatísticas Rápidas
  statsGrid: { display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '1rem' },
  statBox: { background: 'rgba(10, 13, 20, 0.4)', padding: '1.5rem', borderRadius: '8px', textAlign: 'center', border: '1px solid rgba(255,255,255,0.03)' },
  statValue: { fontSize: '2rem', fontWeight: '900', color: '#fff', marginBottom: '0.5rem' },
  statLabel: { fontSize: '0.75rem', color: '#8b978f', letterSpacing: '1px', textTransform: 'uppercase', fontWeight: '600' }
};

export default function DetalhesAgente() {
  const { state } = useLocation();
  const navigate = useNavigate();

  // Proteção: Se o utilizador tentar aceder ao link direto sem passar pela Dashboard
  if (!state || !state.dados) {
    return (
      <div style={{...styles.page, display: 'flex', justifyContent: 'center', alignItems: 'center', flexDirection: 'column', gap: '2rem'}}>
        <h2 style={styles.titulo}>Nenhum dado encontrado</h2>
        <button style={styles.btnVoltar} onClick={() => navigate('/dashboard')}>VOLTAR PARA A DASHBOARD</button>
      </div>
    );
  }

  const { dados, historico, totalKills, totalAssists } = state;
  const agente = dados.melhorAgente;
  const habilidades = getAgenteHabilidades(agente);
  const artwork = getAgenteArtwork(agente);

  // Cálculos baseados nos dados recebidos
  const totalPartidas = historico.filter(p => p.agente === agente).length;
  const taxaVitoriaReal = dados.taxaVitoria;
  const kdaMedio = dados.kdaGeral;

  return (
    <div style={styles.page}>
      <header style={styles.header}>
        <button 
          onClick={() => navigate(-1)} 
          style={styles.btnVoltar}
          onMouseOver={(e) => { e.currentTarget.style.background = 'rgba(255, 70, 85, 0.2)'; e.currentTarget.style.transform = 'translateX(-5px)'; }}
          onMouseOut={(e) => { e.currentTarget.style.background = 'rgba(255, 70, 85, 0.1)'; e.currentTarget.style.transform = 'translateX(0)'; }}
        >
          ← VOLTAR
        </button>
        <h1 style={styles.titulo}>RELATÓRIO DETALHADO: {agente}</h1>
      </header>

      <div style={styles.content}>
        <div style={styles.colArte}>
          <div style={styles.bgGlow} />
          {artwork && <img src={artwork} alt={agente} style={styles.arteFull} />}
        </div>

        <div style={styles.colInfo}>
          {/* VISÃO GERAL */}
          <div style={styles.cardGlass}>
            <span style={styles.secLabel}>DESEMPENHO GERAL DO AGENTE</span>
            <div style={styles.statsGrid}>
              <div style={styles.statBox}>
                <div style={styles.statValue}>{taxaVitoriaReal || '0%'}</div>
                <div style={styles.statLabel}>Taxa de Vitórias</div>
              </div>
              <div style={styles.statBox}>
                <div style={styles.statValue}>{kdaMedio || '0.0'}</div>
                <div style={styles.statLabel}>KDA Médio</div>
              </div>
              <div style={styles.statBox}>
                <div style={styles.statValue}>{totalPartidas || '0'}</div>
                <div style={styles.statLabel}>Partidas Recentes</div>
              </div>
            </div>
          </div>

          {/* EFICÁCIA DAS HABILIDADES */}
          <div style={styles.cardGlass}>
            <span style={styles.secLabel}>EFICÁCIA POR HABILIDADE (IMPACTO ESTIMADO)</span>
            <div style={styles.habGrid}>
              {habilidades.map((hab, index) => {
                // Cálculo para simular o impacto de cada habilidade com base no K/A global
                const multiplicador = [1.2, 0.8, 1.5, 0.5][index] || 1; 
                const valorCalculado = Math.round(((totalKills + totalAssists) / (habilidades.length || 1)) * multiplicador * 10) / 10;
                const maxValor = Math.max(20, valorCalculado * 1.5); // Limite da barra
                
                return (
                  <div key={index} style={styles.habRow}>
                    <div style={styles.habIconBox}>
                      <img src={hab.img} alt={hab.nome} style={styles.habIcon} />
                    </div>
                    <div style={styles.habInfo}>
                      <span style={styles.habNome}>{hab.nome}</span>
                      <div style={styles.barContainer}>
                        <div style={styles.barFundo}>
                          <div style={{ ...styles.barPreen, width: `${Math.min((valorCalculado / maxValor) * 100, 100)}%` }} />
                        </div>
                        <span style={styles.barVal}>{valorCalculado} pts</span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

        </div>
      </div>
    </div>
  );
}