import { useLocation, useNavigate } from 'react-router-dom';
import { getMapaImg } from '../services/assets';

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
  
  gridResumo: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '1.5rem', marginBottom: '2.5rem' },
  cardGlass: { background: 'rgba(22, 27, 34, 0.6)', backdropFilter: 'blur(10px)', WebkitBackdropFilter: 'blur(10px)', padding: '1.5rem', borderRadius: '12px', border: '1px solid rgba(255, 255, 255, 0.05)', boxShadow: '0 10px 30px rgba(0,0,0,0.3)' },
  lblMini: { fontSize: '0.75rem', color: '#8b978f', letterSpacing: '1.5px', textTransform: 'uppercase', fontWeight: '700', display: 'block', marginBottom: '0.25rem' },
  valGrand: { fontSize: '2.2rem', fontWeight: '900', color: '#fff' },
  
  containerMapas: { display: 'flex', flexDirection: 'column', gap: '0.75rem' },
  
  mapaCard: { 
    display: 'flex', 
    alignItems: 'center', 
    background: 'rgba(22, 27, 34, 0.4)', 
    borderRadius: '6px', 
    border: '1px solid rgba(255, 255, 255, 0.05)', 
    overflow: 'hidden', 
    height: '56px', // Altura reduzida para obter a proporção do protótipo
    position: 'relative' 
  },
  
  mapaImgBg: { 
    position: 'absolute', 
    left: 0, 
    top: 0, 
    bottom: 0, 
    width: '220px', // Alargado para o fade ser mais longo e natural
    objectFit: 'cover', 
    maskImage: 'linear-gradient(to right, rgba(0,0,0,1) 40%, rgba(0,0,0,0) 100%)', 
    WebkitMaskImage: 'linear-gradient(to right, rgba(0,0,0,1) 40%, rgba(0,0,0,0) 100%)', 
    opacity: 1, // Aumentado de 0.5 para 1 (Garante visibilidade total)
    zIndex: 1 
  },
  
  barPreen: { 
    position: 'absolute', 
    left: 0, 
    top: 0, 
    bottom: 0, 
    zIndex: 0, 
    transition: 'width 1s ease-in-out' 
  },
  
  infoBlock: { 
    position: 'relative', 
    zIndex: 2, 
    display: 'flex', 
    width: '100%', 
    alignItems: 'center', 
    padding: '0 2rem 0 11rem' 
  },
  
  mapaNome: { 
    fontSize: '1.2rem', 
    fontWeight: '900', 
    letterSpacing: '2px', 
    minWidth: '160px', 
    textShadow: '0 2px 4px rgba(0,0,0,0.8)' 
  },
  
  winTextInline: { 
    color: '#ece8e1', 
    fontSize: '0.95rem', 
    fontWeight: '800', 
    letterSpacing: '1px', 
    flex: 1, 
    textShadow: '0 2px 4px rgba(0,0,0,0.8)' 
  },
  
  metaStats: { 
    display: 'flex', 
    gap: '2rem', 
    justifyContent: 'flex-end', 
    minWidth: '220px', 
    fontSize: '0.95rem', 
    fontWeight: '700', 
    color: '#ece8e1', 
    textShadow: '0 2px 4px rgba(0,0,0,0.8)' 
  }
};

export default function DetalhesMapas() {
  const { state } = useLocation();
  const navigate = useNavigate();

  if (!state || !state.estatisticas) {
    return (
      <div style={{ ...styles.page, display: 'flex', justifyContent: 'center', alignItems: 'center', flexDirection: 'column', gap: '2rem' }}>
        <h2 style={styles.titulo}>Nenhuma estatística encontrada</h2>
        <button style={styles.btnVoltar} onClick={() => navigate('/dashboard')}>VOLTAR PARA A DASHBOARD</button>
      </div>
    );
  }

  const { historico, estatisticas } = state;
  const listaMapas = estatisticas?.porMapa || [];

  // Cálculos de Destaque Global
  const totalPartidasValidas = historico.length;
  const melhorMapaItem = listaMapas[0]?.mapa || '—';
  const melhorMapaWinrate = listaMapas[0]?.taxaVitoria || '0%';

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
        <h1 style={styles.titulo}>ANÁLISE ESTRATÉGICA DE MAPAS</h1>
      </header>

      {/* QUADRO DE RESUMO TÁTICO */}
      <div style={styles.gridResumo}>
        <div style={styles.cardGlass}>
          <span style={styles.lblMini}>Melhor Zona de Combate</span>
          <span style={{ ...styles.valGrand, color: '#ffffff' }}>{melhorMapaItem.toUpperCase()}</span>
        </div>
        <div style={styles.cardGlass}>
          <span style={styles.lblMini}>Maior Taxa de Vitória</span>
          <span style={styles.valGrand}>{melhorMapaWinrate}</span>
        </div>
        <div style={styles.cardGlass}>
          <span style={styles.lblMini}>Amostra</span>
          <span style={{ ...styles.valGrand, color: '#ffffff' }}>{totalPartidasValidas} Confrontos</span>
        </div>
      </div>

      {/* PROJEÇÃO INDIVIDUAL DOS MAPAS */}
      <div style={styles.containerMapas}>
        {listaMapas.map((m, i) => {
          const mapaImg = getMapaImg(m.mapa);
          const pct = parseFloat(m.taxaVitoria) || 0;
          const corBarra = pct >= 60 ? '#0f8a61' : pct >= 40 ? '#0f7f8b' : '#ad2f41';
          
          const totalNoMapa = historico.filter(p => p.mapa?.toLowerCase() === m.mapa?.toLowerCase()).length;
          const vitoriasNoMapa = historico.filter(p => p.mapa?.toLowerCase() === m.mapa?.toLowerCase() && p.resultado === 'Vitoria').length;

          return (
            <div key={i} style={styles.mapaCard}>
              {/* Bloco de progresso sólido que preenche o fundo vertical por completo */}
              <div style={{ ...styles.barPreen, width: `${pct}%`, backgroundColor: corBarra }} />
              
              {mapaImg && <img src={mapaImg} alt={m.mapa} style={styles.mapaImgBg} loading="lazy" />}
              
              <div style={styles.infoBlock}>
                <span style={styles.mapaNome}>{m.mapa?.toUpperCase()}</span>
                
                {/* Texto inline sobreposto à barra */}
                <span style={styles.winTextInline}>WIN {m.taxaVitoria}</span>

                {/* Métricas secundárias mantidas à direita */}
                <div style={styles.metaStats}>
                  <span>JOGOS: <strong style={{ color: '#fff' }}>{totalNoMapa}</strong></span>
                  <span style={{ color: '#ffffff' }}>W: <strong style={{ color: '#fff' }}>{vitoriasNoMapa}</strong></span>
                  <span style={{ color: '#ffffff' }}>L: <strong style={{ color: '#fff' }}>{totalNoMapa - vitoriasNoMapa}</strong></span>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}