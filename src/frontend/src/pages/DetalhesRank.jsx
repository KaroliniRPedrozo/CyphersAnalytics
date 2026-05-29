import { useLocation, useNavigate } from 'react-router-dom';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { getRankImg } from '../services/assets';

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
  btnVoltar: { background: 'transparent', border: '1px solid rgba(255, 255, 255, 0.2)', color: '#ece8e1', padding: '0.6rem 1.2rem', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold', letterSpacing: '1px', transition: 'all 0.3s ease' },
  titulo: { fontSize: '2rem', letterSpacing: '4px', fontWeight: '800', margin: 0, textTransform: 'uppercase' },
  
  layoutGrid: { display: 'flex', gap: '2.5rem', flexWrap: 'wrap', alignItems: 'flex-start' },
  
  // Perfil Lateral
  colRank: { flex: '1', minWidth: '280px', background: 'rgba(22, 27, 34, 0.6)', backdropFilter: 'blur(10px)', border: '1px solid rgba(255,255,255,0.05)', padding: '2rem', borderRadius: '12px', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '1rem' },
  badgeImg: { width: '140px', filter: 'drop-shadow(0 10px 20px rgba(0,255,135,0.15))' },
  rankNome: { fontSize: '1.6rem', fontWeight: '900', letterSpacing: '2px', color: '#fff', textTransform: 'uppercase' },
  
  // Painel Principal
  colGrafico: { flex: '2.5', minWidth: '450px', display: 'flex', flexDirection: 'column', gap: '1.5rem' },
  cardGlass: { background: 'rgba(22, 27, 34, 0.6)', backdropFilter: 'blur(10px)', padding: '2rem', borderRadius: '12px', border: '1px solid rgba(255, 255, 255, 0.05)', boxShadow: '0 10px 30px rgba(0,0,0,0.3)' },
  secLabel: { color: '#ffffff', fontSize: '0.9rem', fontWeight: '800', letterSpacing: '2px', textTransform: 'uppercase', marginBottom: '1.5rem', display: 'block' },
  
  // Mini Quadros de Estatísticas
  miniGrid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '1rem' },
  statBox: { background: 'rgba(10, 13, 20, 0.4)', padding: '1.25rem', borderRadius: '8px', border: '1px solid rgba(255,255,255,0.02)', textAlign: 'center' },
  statVal: { fontSize: '1.8rem', fontWeight: '900', color: '#fff', marginTop: '0.25rem' },
  statLbl: { fontSize: '0.75rem', color: '#8b978f', letterSpacing: '1px', textTransform: 'uppercase', fontWeight: '700' },
  
  customTooltip: { background: '#161b22', border: '1px solid #00ff87', padding: '1rem', borderRadius: '6px', fontSize: '0.85rem', boxShadow: '0 10px 25px rgba(0,0,0,0.5)' }
};

// 1. Tooltip movido para FORA do componente principal para evitar o erro do ESLint
const CustomTooltip = ({ active, payload }) => {
  if (active && payload && payload.length) {
    const data = payload[0].payload;
    return (
      <div style={styles.customTooltip}>
        <p style={{ margin: '0 0 0.5rem 0', fontWeight: 800, color: '#ffffff' }}>CONFRONTO #{data.confronto}</p>
        <p style={{ margin: '0 0 0.25rem 0' }}>Mapa: <strong style={{ color: '#fff' }}>{data.mapa}</strong></p>
        <p style={{ margin: '0 0 0.25rem 0' }}>Agente: <strong style={{ color: '#fff' }}>{data.agente}</strong></p>
        <p style={{ margin: '0 0 0.25rem 0' }}>Desempenho: <strong style={{ color: '#fff' }}>{data.kda} KDA</strong></p>
        <p style={{ margin: 0, color: data.resultado === 'VITÓRIA' ? '#00ff87' : '#ff4655', fontWeight: 900 }}>{data.resultado}</p>
      </div>
    );
  }
  return null;
};

// 1.5 Renderizador dos pontos do gráfico (Vermelho para Derrota, Verde para Vitória)
const CustomDot = (props) => {
  const { cx, cy, payload } = props;
  
  if (!cx || !cy) return null;

  const isVitoria = payload.resultado === 'VITÓRIA';
  const cor = isVitoria ? '#00ff87' : '#ff4655';

  return (
    <circle 
      cx={cx} 
      cy={cy} 
      r={5} 
      stroke={cor} 
      strokeWidth={3} 
      fill="#161b22"
    />
  );
};

// 2. Componente Principal
export default function DetalhesRank() {
  const { state } = useLocation();
  const navigate = useNavigate();

  if (!state || !state.dados) {
    return (
      <div style={{...styles.page, display: 'flex', justifyContent: 'center', alignItems: 'center', flexDirection: 'column', gap: '2rem'}}>
        <h2 style={styles.titulo}>Nenhum dado competitivo encontrado</h2>
        <button style={styles.btnVoltar} onClick={() => navigate('/dashboard')}>VOLTAR PARA A DASHBOARD</button>
      </div>
    );
  }

  const { dados, historico } = state;
  const rankImg = getRankImg(dados?.rankAtual);

  const dadosCronologicos = [...historico].reverse().map((p, idx) => ({
    confronto: idx + 1,
    kda: p.kda,
    mapa: p.mapa?.toUpperCase(),
    agente: p.agente,
    resultado: p.resultado === 'Vitoria' ? 'VITÓRIA' : 'DERROTA'
  }));

  let winStreak = 0;
  for (let i = 0; i < historico.length; i++) {
    if (historico[i].resultado === 'Vitoria') {
      winStreak++;
    } else {
      break;
    }
  }

  const maiorKDA = dadosCronologicos.length > 0 
    ? Math.max(...dadosCronologicos.map(p => p.kda)) 
    : '0.0';

  return (
    <div style={styles.page}>
      <header style={styles.header}>
        <button 
          onClick={() => navigate('/dashboard')} 
          style={styles.btnVoltar}
          onMouseOver={(e) => { e.currentTarget.style.background = 'rgba(255, 255, 255, 0.1)'; e.currentTarget.style.transform = 'translateX(-5px)'; }}
          onMouseOut={(e) => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.transform = 'translateX(0)'; }}
        >
          ← VOLTAR
        </button>
        <h1 style={styles.titulo}>HISTÓRICO DE EVOLUÇÃO COMPETITIVA</h1>
      </header>

      <div style={styles.layoutGrid}>
        <div style={styles.colRank}>
          <span style={{ fontSize: '0.8rem', color: '#8b978f', letterSpacing: '2px', fontWeight: 700 }}>TIER ATUAL DE COMPETIÇÃO</span>
          {rankImg && <img src={rankImg} alt={dados?.rankAtual} style={styles.badgeImg} />}
          <span style={styles.rankNome}>{dados?.rankAtual || 'UNRANKED'}</span>
          <div style={{ width: '100%', height: '1px', background: 'rgba(255,255,255,0.05)', margin: '0.5rem 0' }} />
          <span style={{ fontSize: '0.85rem', color: '#8b978f' }}>Taxa de vitória global na temporada: <strong>{dados?.taxaVitoria || '0%'}</strong></span>
        </div>

        <div style={styles.colGrafico}>
          
          <div style={styles.cardGlass}>
            <span style={styles.secLabel}>CURVA DE DESEMPENHO EM KDA (CRONOLÓGICO)</span>
            <div style={{ width: '100%', height: 320, marginTop: '1rem' }}>
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={dadosCronologicos} margin={{ top: 10, right: 10, left: -30, bottom: 0 }}>
                  <defs>
                    <linearGradient id="rankGlow" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#ffffff" stopOpacity={0.4} />
                      <stop offset="95%" stopColor="#ffffff" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.03)" vertical={false} />
                  <XAxis dataKey="confronto" stroke="#4b5563" tickLine={false} dy={10} style={{ fontSize: '0.8rem' }} />
                  <YAxis stroke="#4b5563" tickLine={false} dx={-5} style={{ fontSize: '0.8rem' }} />
                  <Tooltip content={<CustomTooltip />} />
                  <Area 
                    type="monotone" 
                    dataKey="kda" 
                    stroke="#ffffff"
                    strokeWidth={3} 
                    fill="url(#rankGlow)" 
                    dot={<CustomDot />}
                    activeDot={(props) => {
                      const { cx, cy, payload } = props;
                      const cor = payload.resultado === 'VITÓRIA' ? '#00ff87' : '#ff4655';
                      return <circle cx={cx} cy={cy} r={8} fill={cor} stroke="#fff" strokeWidth={2} />;
                    }}
                  />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div style={styles.cardGlass}>
            <span style={styles.secLabel}>METRICAS DE PROGRESSÃO RECENTES</span>
            <div style={styles.miniGrid}>
              <div style={styles.statBox}>
                <span style={styles.statLbl}>Sequência Atual</span>
                <div style={{ ...styles.statVal, color: winStreak > 0 ? '#ffffff' : '#fff' }}>
                  {winStreak} {winStreak === 1 ? 'Vitória' : 'Vitórias'}
                </div>
              </div>
              <div style={styles.statBox}>
                <span style={styles.statLbl}>Maior KDA Registrado</span>
                <div style={{ ...styles.statVal, color: '#ffffff' }}>{maiorKDA}</div>
              </div>
              <div style={styles.statBox}>
                <span style={styles.statLbl}>Amostra de Análise</span>
                <div style={styles.statVal}>{dadosCronologicos.length} Jogos</div>
              </div>
            </div>
          </div>

        </div>
      </div>
    </div>
  );
}