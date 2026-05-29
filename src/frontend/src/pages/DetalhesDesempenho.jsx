import { useLocation, useNavigate } from 'react-router-dom';

const styles = {
  page: { 
    boxSizing: 'border-box',
    background: 'radial-gradient(circle at top left, #161b22 0%, #0a0d14 100%)', 
    height: '100vh', 
    color: '#ece8e1', 
    padding: '3rem 5% 5rem 5%', 
    fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
    overflowY: 'auto'
  },
  header: { display: 'flex', alignItems: 'center', gap: '2rem', marginBottom: '3rem', borderBottom: '1px solid rgba(255, 255, 255, 0.05)', paddingBottom: '1.5rem' },
  btnVoltar: { background: 'rgba(236, 232, 225, 0.05)', border: '1px solid rgba(236, 232, 225, 0.2)', color: '#ece8e1', padding: '0.6rem 1.2rem', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold', letterSpacing: '1px', transition: 'all 0.3s ease' },
  titulo: { fontSize: '2rem', letterSpacing: '4px', fontWeight: '800', margin: 0, textTransform: 'uppercase' },
  
  gridPrincipal: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '2rem', marginTop: '1rem' },
  cardGlass: { background: 'rgba(22, 27, 34, 0.6)', backdropFilter: 'blur(10px)', WebkitBackdropFilter: 'blur(10px)', padding: '2rem', borderRadius: '12px', border: '1px solid rgba(255, 255, 255, 0.05)', boxShadow: '0 10px 30px rgba(0,0,0,0.3)' },
  secLabel: { color: '#00e5ff', fontSize: '0.9rem', fontWeight: '800', letterSpacing: '2px', textTransform: 'uppercase', marginBottom: '1.5rem', display: 'block' },
  
  // Grandes Números
  destaqueRow: { display: 'flex', justifyContent: 'space-around', alignItems: 'center', gap: '2rem', padding: '1rem 0' },
  blocoGrande: { textAlign: 'center' },
  numeroGigante: { fontSize: '4rem', fontWeight: '900', color: '#fff', lineHeight: '1', textShadow: '0 4px 20px rgba(255,255,255,0.05)' },
  numeroKDR: { fontSize: '4rem', fontWeight: '900', color: '#ff4655', lineHeight: '1' },
  numeroACS: { fontSize: '4rem', fontWeight: '900', color: '#00ff87', lineHeight: '1' },
  labelGrande: { fontSize: '0.85rem', color: '#8b978f', letterSpacing: '2px', fontWeight: '700', marginTop: '0.5rem', textTransform: 'uppercase' },
  
  // Listas Analíticas
  dataRow: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem 1.2rem', background: 'rgba(10, 13, 20, 0.4)', borderRadius: '8px', marginBottom: '0.75rem', border: '1px solid rgba(255,255,255,0.02)' },
  dataLabel: { fontSize: '1rem', color: '#8b978f', fontWeight: '600', letterSpacing: '0.5px' },
  dataValue: { fontSize: '1.15rem', color: '#fff', fontWeight: '800' },
  
  // Barras de Comparação
  barContainer: { marginTop: '1rem' },
  barFundo: { height: '8px', background: 'rgba(255,255,255,0.05)', borderRadius: '4px', overflow: 'hidden', marginTop: '0.5rem' },
  barPreen: { height: '100%', borderRadius: '4px' }
};

export default function DetalhesDesempenho() {
  const { state } = useLocation();
  const navigate = useNavigate();

  if (!state || !state.dados) {
    return (
      <div style={{...styles.page, display: 'flex', justifyContent: 'center', alignItems: 'center', flexDirection: 'column', gap: '2rem'}}>
        <h2 style={styles.titulo}>Nenhum dado encontrado</h2>
        <button style={styles.btnVoltar} onClick={() => navigate('/dashboard')}>VOLTAR PARA A DASHBOARD</button>
      </div>
    );
  }

  const { dados, historico, totalKills, totalAssists, avgACS } = state;
  
  // Computação de médias com base nas partidas trazidas
  const partidasRespondidas = historico.length || 1;
  const mediaKills = (totalKills / partidasRespondidas).toFixed(1);
  const mediaAssists = (totalAssists / partidasRespondidas).toFixed(1);
  
  // Métricas calculadas para preenchimento de campos avançados
  const partidasGanhas = historico.filter(p => p.resultado === 'Vitoria').length;
  const taxaVitoriaGeral = ((partidasGanhas / partidasRespondidas) * 100).toFixed(1) + '%';

  return (
    <div style={styles.page}>
      <header style={styles.header}>
        <button 
          onClick={() => navigate(-1)} 
          style={styles.btnVoltar}
          onMouseOver={(e) => { e.currentTarget.style.background = 'rgba(0, 229, 255, 0.2)'; e.currentTarget.style.transform = 'translateX(-5px)'; }}
          onMouseOut={(e) => { e.currentTarget.style.background = 'rgba(0, 229, 255, 0.1)'; e.currentTarget.style.transform = 'translateX(0)'; }}
        >
          ← VOLTAR
        </button>
        <h1 style={styles.titulo}>ANÁLISE DE PERFORMANCE GLOBAL</h1>
      </header>

      <div style={styles.gridPrincipal}>
        
        {/* CARD 1: MEDIDORES CRÍTICOS */}
        <div style={styles.cardGlass}>
          <span style={styles.secLabel}>COMBAT RATINGS</span>
          <div style={styles.destaqueRow}>
            <div style={styles.blocoGrande}>
              <div style={styles.numeroKDR}>{dados?.kdaGeral || '—'}</div>
              <div style={styles.labelGrande}>KDR GLOBAL</div>
            </div>
            <div style={styles.blocoGrande}>
              <div style={styles.numeroACS}>{avgACS || '—'}</div>
              <div style={styles.labelGrande}>MÉDIA ACS</div>
            </div>
          </div>
          
          <div style={{ marginTop: '1.5rem' }}>
            <div style={styles.barContainer}>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem' }}>
                <span style={{ color: '#8b978f' }}>Taxa de Vitória Geral</span>
                <span style={{ fontWeight: '700' }}>{taxaVitoriaGeral}</span>
              </div>
              <div style={styles.barFundo}>
                <div style={{ ...styles.barPreen, width: taxaVitoriaGeral, background: '#00ff87' }} />
              </div>
            </div>
          </div>
        </div>

        {/* CARD 2: MÉDIAS POR PARTIDA */}
        <div style={styles.cardGlass}>
          <span style={styles.secLabel}>MÉDIAS POR CONFRONTO</span>
          
          <div style={styles.dataRow}>
            <span style={styles.dataLabel}>Abates (Kills) Médios</span>
            <span style={styles.dataValue}>{mediaKills}</span>
          </div>
          
          <div style={styles.dataRow}>
            <span style={styles.dataLabel}>Assistências Médias</span>
            <span style={styles.dataValue}>{mediaAssists}</span>
          </div>

          <div style={styles.dataRow}>
            <span style={styles.dataLabel}>Total de Abates Computados</span>
            <span style={styles.dataValue}>{totalKills}</span>
          </div>
        </div>

        {/* CARD 3: ACURÁCIA E PRECISÃO */}
        <div style={styles.cardGlass}>
          <span style={styles.secLabel}>// REGISTROS DE COMBATE</span>
          
          <div style={styles.dataRow}>
            <span style={styles.dataLabel}>Headshot %</span>
            <span style={styles.dataValue}>—</span>
          </div>
          
          <div style={styles.dataRow}>
            <span style={styles.dataLabel}>First Bloods</span>
            <span style={styles.dataValue}>—</span>
          </div>

          <div style={styles.dataRow}>
            <span style={styles.dataLabel}>Amostra de Histórico</span>
            <span style={styles.dataValue}>{historico.length} Partidas</span>
          </div>
        </div>

      </div>
    </div>
  );
}