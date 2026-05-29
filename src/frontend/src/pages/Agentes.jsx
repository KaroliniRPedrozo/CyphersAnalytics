import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import { agenteIcones, getAgenteIcone, getAgenteHabilidades, getAgenteArtwork } from '../services/assets';
import dadosHabilidades from '../assets/valorant_agents_abilities.json';
import agentColors from '../assets/agentes_valorant_cores.json';

const styles = {
  page: { 
    boxSizing: 'border-box',
    background: 'radial-gradient(circle at top center, #161b22 0%, #0a0d14 100%)', 
    height: '100vh', 
    overflowY: 'auto', 
    color: '#ece8e1', 
    padding: '3rem 5% 5rem 5%', 
    fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"
  },
  
  header: { 
    display: 'flex', 
    alignItems: 'center', 
    gap: '2rem', 
    marginBottom: '3rem',
    borderBottom: '1px solid rgba(255, 255, 255, 0.05)',
    paddingBottom: '1.5rem'
  },
  
  btnVoltar: { 
    background: 'rgba(255, 70, 85, 0.1)', 
    border: '1px solid rgba(255, 70, 85, 0.5)', 
    color: '#ff4655', 
    padding: '0.6rem 1.2rem', 
    borderRadius: '4px',
    cursor: 'pointer', 
    fontWeight: 'bold', 
    fontFamily: 'inherit',
    letterSpacing: '1px',
    transition: 'all 0.3s ease'
  },
  
  titulo: { fontSize: '2rem', letterSpacing: '4px', fontWeight: '800', margin: 0, textShadow: '0 2px 10px rgba(0,0,0,0.5)' },
  subtitulo: { fontSize: '1.1rem', color: '#8b978f', marginBottom: '2rem', letterSpacing: '2px', fontWeight: '600' },
  
  grid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(130px, 1fr))', gap: '1.2rem' },
  
  card: { 
    boxSizing: 'border-box',
    background: 'rgba(22, 27, 34, 0.6)', 
    backdropFilter: 'blur(10px)',
    WebkitBackdropFilter: 'blur(10px)',
    padding: '1.2rem 1rem',
    borderRadius: '8px', 
    textAlign: 'center', 
    display: 'flex', 
    flexDirection: 'column', 
    alignItems: 'center', 
    gap: '0.8rem', 
    cursor: 'pointer', 
    border: '1px solid rgba(255, 255, 255, 0.05)', 
    transition: 'all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1)',
    boxShadow: '0 4px 6px rgba(0,0,0,0.3)'
  },
  
  icone: { width: '60px', height: '60px', borderRadius: '50%', objectFit: 'cover', border: '2px solid rgba(255,255,255,0.1)', background: '#111' },
  nomeAgente: { fontSize: '0.9rem', fontWeight: '700', letterSpacing: '1px', color: '#ece8e1' },
  
  gridDesativados: { display: 'grid', gridTemplateColumns: '1fr', gap: '1.5rem' },
  cardDesativado: { 
    boxSizing: 'border-box',
    background: 'rgba(255, 70, 85, 0.05)', 
    backdropFilter: 'blur(5px)',
    border: '1px solid rgba(255, 70, 85, 0.3)', 
    padding: '2rem', 
    borderRadius: '8px', 
    display: 'flex', 
    gap: '2rem', 
    alignItems: 'center', 
    cursor: 'not-allowed' 
  },
  iconeDesativado: { width: '70px', height: '70px', filter: 'grayscale(100%)', opacity: 0.5, borderRadius: '50%' },
  infoDesativado: { display: 'flex', flexDirection: 'column', gap: '0.5rem' },
  nomeDesativado: { fontSize: '1.2rem', fontWeight: '900', color: '#ff4655', letterSpacing: '2px' },
  motivo: { fontSize: '0.95rem', color: '#aaa', margin: 0, lineHeight: '1.5' },

  // --- ESTILOS DO MODAL CORRIGIDOS (Compactos e sem rolagem) ---
  modalOverlay: { position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(0, 0, 0, 0.85)', backdropFilter: 'blur(10px)', padding: '2rem' },
  
  // Alturas controladas para evitar o overflow
  modalContent: { position: 'relative', width: '100%', maxWidth: '1000px', height: '75vh', minHeight: '550px', maxHeight: '650px', borderRadius: '16px', overflow: 'hidden', display: 'flex', background: '#0a0d14', boxShadow: '0 20px 50px rgba(0,0,0,0.8)' },
  
  // Opacidade ajustada para a imagem webp aparecer perfeitamente
  bgLayer: { position: 'absolute', inset: 0, backgroundSize: 'cover', backgroundPosition: 'center', opacity: 0.7, mixBlendMode: 'screen' },
  colorOverlay: { position: 'absolute', inset: 0, zIndex: 1 },
  
  closeBtn: { position: 'absolute', top: '1.5rem', right: '1.5rem', zIndex: 10, background: 'rgba(255,255,255,0.1)', border: '1px solid rgba(255,255,255,0.2)', color: '#fff', width: '40px', height: '40px', borderRadius: '50%', fontSize: '1.2rem', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', transition: 'all 0.2s ease' },
  
  gridContainer: { position: 'relative', zIndex: 2, display: 'flex', width: '100%', height: '100%' },
  
  colLeft: { flex: '1', display: 'flex', alignItems: 'flex-end', justifyContent: 'center', position: 'relative', overflow: 'hidden' },
  agentArtwork: { width: '140%', height: '110%', objectFit: 'contain', objectPosition: 'bottom center', transform: 'translateY(2%)' },
  
  // Retirado o overflowY e reduzidos os espaçamentos
  colRight: { flex: '1.2', display: 'flex', flexDirection: 'column', justifyContent: 'center', padding: '2rem 3rem 2rem 0', overflow: 'hidden' },
  
  roleBox: { display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.25rem' },
  roleName: { color: '#ece8e1', fontSize: '0.9rem', fontWeight: '800', letterSpacing: '3px', opacity: 0.8 },
  
  agentNameModal: { fontSize: '3.8rem', fontWeight: '900', letterSpacing: '3px', margin: '0 0 0.5rem 0', lineHeight: '1', textShadow: '0 4px 15px rgba(0,0,0,0.5)' },
  
  bio: { color: '#ece8e1', fontSize: '0.9rem', lineHeight: '1.5', fontWeight: '500', marginBottom: '1.5rem', textShadow: '0 2px 4px rgba(0,0,0,0.8)' },
  
  skillsContainer: { display: 'flex', flexDirection: 'column', gap: '0.6rem' },
  skillBox: { display: 'flex', alignItems: 'center', gap: '1rem', background: 'rgba(10, 13, 20, 0.75)', padding: '0.75rem 1rem', borderRadius: '8px', border: '1px solid', backdropFilter: 'blur(5px)' },
  
  // Fundo do ícone agora é escuro, para ícones brancos aparecerem sempre!
  skillIconBox: { width: '48px', height: '48px', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, background: 'rgba(0,0,0,0.6)', boxShadow: '0 4px 10px rgba(0,0,0,0.5)' },
  skillIconModal: { width: '28px', height: '28px', objectFit: 'contain' },
  
  skillText: { flex: 1 },
  skillTitle: { margin: '0 0 0.2rem 0', color: '#fff', fontSize: '1rem', fontWeight: '800', letterSpacing: '1px' },
  
  // Limita a descrição a 2 linhas para garantir que não empurra o layout
  skillDesc: { margin: 0, color: '#8b978f', fontSize: '0.8rem', lineHeight: '1.3', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }
};

export default function Agentes() {
  const navigate = useNavigate();
  const [agentesDesativados, setAgentesDesativados] = useState([]);
  const [loading, setLoading] = useState(true);
  const [agenteWiki, setAgenteWiki] = useState(null);

  const todosAgentes = Object.keys(agenteIcones);

  useEffect(() => {
    async function carregarStatus() {
      try {
        const { data } = await api.get('/Status/agentes-desativados');
        setAgentesDesativados(data);
      } catch (error) {
        console.error("Erro ao buscar status:", error);
      } finally {
        setLoading(false);
      }
    }
    carregarStatus();
  }, []);

  const agentesAtivos = todosAgentes.filter(a => !agentesDesativados.find(d => d.nome === a));

  const procurarDescricaoHabilidade = (nomeAgente, nomeHabilidade) => {
    if (!dadosHabilidades || !dadosHabilidades.valorant_agents) return "Descrição indisponível.";
    const roles = dadosHabilidades.valorant_agents.roles;
    for (const roleKey in roles) {
      const agente = roles[roleKey].agents.find(a => a.name.toLowerCase() === nomeAgente.toLowerCase());
      if (agente) {
        for (const habKey in agente.abilities) {
          const nomeNoJson = agente.abilities[habKey].name.toLowerCase().replace(/[^a-z0-9]/g, '');
          const nomeProcurado = nomeHabilidade.toLowerCase().replace(/[^a-z0-9]/g, '');
          if (nomeNoJson === nomeProcurado) {
            return agente.abilities[habKey].description;
          }
        }
      }
    }
    return "Descrição não encontrada para esta habilidade.";
  };

  const procurarInfoAgente = (nomeAgente) => {
    if (!dadosHabilidades || !dadosHabilidades.valorant_agents) return { descricao: "", role: "DESCONHECIDO" };
    const roles = dadosHabilidades.valorant_agents.roles;
    for (const roleKey in roles) {
      const agente = roles[roleKey].agents.find(a => a.name.toLowerCase() === nomeAgente.toLowerCase());
      if (agente) {
        return {
          descricao: agente.description || " ", // Evita que a descrição vazia quebre o layout do modal(Bio do agente)
          role: roleKey
        };
      }
    }
    return { descricao: "Dados não encontrados.", role: "AGENTE" };
  };

  if (loading) return <div style={styles.page}>Carregando status do servidor...</div>;

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
        <h1 style={styles.titulo}>CENTRAL DE AGENTES</h1>
      </header>

      <main style={styles.container}>
        <section>
          <h2 style={styles.subtitulo}>AGENTES ATIVOS</h2>
          <div style={styles.grid}>
            {agentesAtivos.map(agente => (
              <div 
                key={agente} 
                style={styles.card}
                onClick={() => setAgenteWiki(agente)} 
                onMouseOver={(e) => {
                  e.currentTarget.style.borderColor = 'rgba(255, 70, 85, 0.5)';
                  e.currentTarget.style.transform = 'translateY(-8px)';
                  e.currentTarget.style.boxShadow = '0 15px 25px rgba(255, 70, 85, 0.15)';
                  e.currentTarget.style.background = 'rgba(22, 27, 34, 0.8)';
                }}
                onMouseOut={(e) => {
                  e.currentTarget.style.borderColor = 'rgba(255, 255, 255, 0.05)';
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = '0 4px 6px rgba(0,0,0,0.3)';
                  e.currentTarget.style.background = 'rgba(22, 27, 34, 0.6)';
                }}
              >
                <img src={getAgenteIcone(agente)} alt={agente} style={styles.icone} />
                <span style={styles.nomeAgente}>{agente.toUpperCase()}</span>
              </div>
            ))}
          </div>
        </section>

        {agentesDesativados.length > 0 && (
          <section style={{ marginTop: '4rem' }}>
            <h2 style={{ ...styles.subtitulo, color: '#ff4655' }}>AGENTES DESATIVADOS</h2>
            <div style={styles.gridDesativados}>
              {agentesDesativados.map(agente => (
                <div key={agente.nome} style={styles.cardDesativado}>
                  <img src={getAgenteIcone(agente.nome)} alt={agente.nome} style={styles.iconeDesativado} />
                  <div style={styles.infoDesativado}>
                    <span style={styles.nomeDesativado}>{agente.nome.toUpperCase()}</span>
                    <p style={styles.motivo}>{agente.motivo}</p>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}
      </main>

      {agenteWiki && (() => {
        const cores = agentColors.find(a => a.nome.toLowerCase() === agenteWiki.toLowerCase())?.cores 
          || { primaria: '#1c212e', secundaria: '#ff4655', detalhe: '#ece8e1' };
        
        // Caminho da imagem: Certifique-se de que o nome do ficheiro (ex: "Gekko_Background.webp") respeite exatamente as maiúsculas/minúsculas.
        const bgImage = new URL(`../assets/${agenteWiki}_Background.webp`, import.meta.url).href;
        
        const infoAgente = procurarInfoAgente(agenteWiki);

        return (
          <div style={styles.modalOverlay} onClick={() => setAgenteWiki(null)}>
            <div style={styles.modalContent} onClick={(e) => e.stopPropagation()}>
              
              <div style={{ ...styles.bgLayer, backgroundImage: `url(${bgImage})` }} />
              
              {/* Novo Gradiente: Muito mais transparente à esquerda para mostrar o Webp, escurecendo na direita para leitura */}
              <div style={{ ...styles.colorOverlay, background: `linear-gradient(90deg, ${cores.primaria}66 0%, rgba(10, 13, 20, 0.98) 60%)` }} />

              <button 
                style={styles.closeBtn} 
                onClick={() => setAgenteWiki(null)}
                onMouseOver={(e) => { e.currentTarget.style.background = 'rgba(255,255,255,0.2)'; }}
                onMouseOut={(e) => { e.currentTarget.style.background = 'rgba(255,255,255,0.1)'; }}
              >✕</button>

              <div style={styles.gridContainer}>
                
                <div style={styles.colLeft}>
                  <img 
                    src={getAgenteArtwork(agenteWiki) || getAgenteIcone(agenteWiki)} 
                    alt={agenteWiki} 
                    style={{ ...styles.agentArtwork, filter: `drop-shadow(0 0 25px ${cores.primaria})` }} 
                  />
                </div>

                <div style={styles.colRight}>
                  
                  <div style={styles.roleBox}>
                    <span style={styles.roleName}>{infoAgente.role.toUpperCase()}</span>
                  </div>

                  <h2 style={{ ...styles.agentNameModal, color: cores.secundaria }}>
                    {agenteWiki.toUpperCase()}
                  </h2>
                  
                  {/* Linha de separação corrigida com fallback de cores para garantir que sempre apareça */}
                  <div style={{ 
                    width: '70%', // Um pouco mais curta para dar um efeito de sublinhado elegante
                    height: '2px', // Ligeiramente mais espessa (2px em vez de 1px) para destacar mais
                    background: `linear-gradient(90deg, ${cores.detalhe || cores.secundaria || '#ece8e1'}, transparent)`, 
                    marginBottom: '1.2rem',
                    borderRadius: '2px'
                  }} />

                  <p style={styles.bio}>{infoAgente.descricao}</p>

                  <div style={styles.skillsContainer}>
                    {getAgenteHabilidades(agenteWiki).map((hab, index) => (
                      <div key={index} style={{ ...styles.skillBox, borderColor: `${cores.primaria}44` }}>
                        
                        {/* Fundo Escuro com borda da cor primária */}
                        <div style={{ ...styles.skillIconBox, border: `1px solid ${cores.primaria}` }}>
                          <img src={hab.img} alt={hab.nome} style={styles.skillIconModal} onError={(e) => e.target.style.display = 'none'} />
                        </div>
                        
                        <div style={styles.skillText}>
                          <h4 style={styles.skillTitle}>{hab.nome.toUpperCase()}</h4>
                          <p style={styles.skillDesc}>{procurarDescricaoHabilidade(agenteWiki, hab.nome)}</p>
                        </div>
                      </div>
                    ))}
                  </div>

                </div>
              </div>
            </div>
          </div>
        );
      })()}
    </div>
  );
}