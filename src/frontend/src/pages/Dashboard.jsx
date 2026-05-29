import { useState, useEffect, useMemo, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import api from '../services/api';
import { getRankImg, getAgenteCard, getAgenteIcone, getMapaImg, getAgenteHabilidades, getAgenteArtwork } from '../services/assets';
import logoValorant from '../assets/logo-valorant.png';
import skullIcon from '../assets/skull-icon.webp';
import bgAgents from '../assets/bg-agents.jpg';

export default function Dashboard() {
  const navigate = useNavigate();
  const gameName = localStorage.getItem('gameName');
  const tagLine  = localStorage.getItem('tagLine');

  const [dados, setDados]         = useState(null);
  const [historico, setHistorico] = useState([]);
  const [estatisticas, setEstat]  = useState(null);
  const [modoAtivo, setModoAtivo] = useState('todos');
  const [modos, setModos]         = useState([]);
  const [loading, setLoading]     = useState(true);
  const [erro, setErro]           = useState('');
  const [act, setAct]             = useState(1);

  // Função de carregamento memorizada para evitar loops com o useEffect e carregamento com Cache (SessionStorage) para evitar Erro 429
  const carregarDados = useCallback(async () => {
    try {
      setLoading(true);

      // 1. Verifica se já temos os dados salvos nos últimos 3 minutos (180000 milissegundos)
      const cacheKey = `dash_${gameName}_${tagLine}`;
      const cachedData = sessionStorage.getItem(cacheKey);
      const cacheTime = sessionStorage.getItem(`${cacheKey}_time`);

      if (cachedData && cacheTime && (Date.now() - parseInt(cacheTime) < 180000)) {
        const parsed = JSON.parse(cachedData);
        setDados(parsed.dados);
        setHistorico(parsed.historico);
        setEstat(parsed.estatisticas);
        setAct(parsed.act);
        setModos(parsed.modos);
        setLoading(false);
        return; // Sai da função mais cedo e não faz requisições na API!
      }

      // 2. Se não tem cache (ou expirou), faz as requisições normais
      const resDados = await api.get(`/Jogadores/${gameName}/${tagLine}`);

      const [resHistorico, resEstat, resTemporada] = await Promise.all([
        api.get(`/Jogadores/${gameName}/${tagLine}/historico?size=10`),
        api.get(`/Jogadores/${gameName}/${tagLine}/estatisticas`),
        api.get(`/Jogadores/temporada`),
      ]);

      const modosUnicos = [...new Set((resHistorico.data.partidas || []).map(p => p.modo))].filter(Boolean);

      // 3. Atualiza os estados da tela
      setDados(resDados.data); 
      setHistorico(resHistorico.data.partidas || []);
      setEstat(resEstat.data);
      setAct(resTemporada.data?.act || 1);
      setModos(modosUnicos);

      // 4. Salva o resultado no Cache para as próximas vezes que você voltar para a página
      const novosDadosCache = { 
        dados: resDados.data, 
        historico: resHistorico.data.partidas || [], 
        estatisticas: resEstat.data, 
        act: resTemporada.data?.act || 1, 
        modos: modosUnicos 
      };
      sessionStorage.setItem(cacheKey, JSON.stringify(novosDadosCache));
      sessionStorage.setItem(`${cacheKey}_time`, Date.now().toString());

    } catch (err) {
      console.error('Erro ao carregar dados:', err);
      setErro('Erro ao carregar dados. Tente novamente.');
    } finally {
      setLoading(false);
    }
  }, [gameName, tagLine]);

  // Função para Deletar a Partida com confirmação
  const deletarPartida = async (e, id) => {
    e.stopPropagation(); 
    if (window.confirm('Tem certeza que deseja deletar esta partida?')) {
      try {
        await api.delete(`/Partidas/${id}`);
        const cacheKey = `dash_${gameName}_${tagLine}`;
        sessionStorage.removeItem(cacheKey);
        carregarDados();
      } catch (err) {
        console.error(err);
        alert('Erro ao deletar a partida.');
      }
    }
  };

  useEffect(() => {
    if (!gameName || !tagLine) { navigate('/'); return; }
    carregarDados();
  }, [carregarDados, gameName, navigate, tagLine]);

  function logout() { localStorage.clear(); navigate('/'); }

  const partidasFiltradas = modoAtivo === 'todos'
    ? historico
    : historico.filter(p => p.modo === modoAtivo);

  const dadosGrafico  = [...partidasFiltradas].reverse().map((p, i) => ({ name: i + 1, kda: p.kda }));
  const melhoresMapas = estatisticas?.porMapa?.slice(0, 3) || [];
  const rankImg       = getRankImg(dados?.rankAtual);
  const agenteCard    = getAgenteCard(dados?.melhorAgente);
  const agenteArtwork = getAgenteArtwork(dados?.melhorAgente);
  const habilidades   = getAgenteHabilidades(dados?.melhorAgente || '');
  
  const totalKills    = useMemo(() => historico.reduce((a, p) => a + p.kills, 0), [historico]);
  const totalAssists  = useMemo(() => historico.reduce((a, p) => a + p.assists, 0), [historico]);
  const avgACS = useMemo(() => historico.length > 0
    ? Math.round(historico.reduce((a, p) => a + Math.round(p.score / 20), 0) / historico.length)
    : 0, [historico]);

  if (loading) return (
    <div style={s.loadingPage}>
      <img src={logoValorant} alt="" style={{ width: 56, marginBottom: '1rem' }} />
      <p style={{ color: '#ff4655', fontWeight: 700, letterSpacing: 3, fontSize: '0.8rem' }}>CARREGANDO...</p>
    </div>
  );

  if (erro) return (
    <div style={s.loadingPage}>
      <p style={{ color: '#ff4655', marginBottom: '1rem' }}>{erro}</p>
      <button onClick={carregarDados} style={s.btnRetry}>Tentar novamente</button>
    </div>
  );

  return (
    <div style={s.page}>
      <div style={s.bgLayer} />

      {/* TOPBAR — Menu de Filtros + Botão Central + Perfil */}
      <header style={s.topbar}>
        <nav style={s.filterContainer}>
          <button 
            style={{ ...s.btnFiltro, ...(modoAtivo === 'todos' ? s.btnFiltroAtivo : {}) }}
            onClick={() => setModoAtivo('todos')}
            onMouseOver={(e) => { if(modoAtivo !== 'todos') e.currentTarget.style.color = '#fff' }}
            onMouseOut={(e) => { if(modoAtivo !== 'todos') e.currentTarget.style.color = '#8b978f' }}
          >
            TODOS
          </button>
          {modos.map(m => (
            <button 
              key={m} 
              style={{ ...s.btnFiltro, ...(modoAtivo === m ? s.btnFiltroAtivo : {}) }} 
              onClick={() => setModoAtivo(m)}
              onMouseOver={(e) => { if(modoAtivo !== m) e.currentTarget.style.color = '#fff' }}
              onMouseOut={(e) => { if(modoAtivo !== m) e.currentTarget.style.color = '#8b978f' }}
            >
              {m.toUpperCase()}
            </button>
          ))}
        </nav>
        
        <div style={s.topbarRight}>
          <div style={s.filterContainer}>
            <button 
              style={s.btnFiltro} 
              onClick={() => navigate('/agentes')}
              onMouseOver={(e) => { e.currentTarget.style.color = '#fff' }}
              onMouseOut={(e) => { e.currentTarget.style.color = '#8b978f' }}
            >
              CENTRAL DE AGENTES
            </button>
          </div>

          <div style={s.playerTag} title="O seu Perfil">
            <span style={s.playerNome}>{gameName} #{tagLine}</span>
            <img src={logoValorant} alt="Logo" style={{ width: 16, opacity: 0.5 }} />
          </div>
        </div>
      </header>

      <div style={s.contentWrapper}>
        <aside style={s.sidebar}>
          <div style={{ ...s.sTop, position:'relative', zIndex:2 }}>
            <img src={logoValorant} alt="Logo" style={s.sLogo} />
            <div style={s.sTemporada}>
              <span>TEMPORADA 2026</span>
              <span>ATO {act}</span>
            </div>
          </div>

          <div style={s.sAgenteCard}>
            {agenteCard
              ? <img src={agenteCard} alt={dados?.melhorAgente} style={s.sAgenteImg} loading="lazy" />
              : <div style={s.sAgenteVazio}>{dados?.melhorAgente?.[0] || '?'}</div>
            }
          </div>

          <div style={s.sRankBox}>
            {rankImg && <img src={rankImg} alt={dados?.rankAtual} style={s.sRankImg} loading="lazy" />}
            <span style={s.sRankNome}>{dados?.rankAtual?.toUpperCase() || 'UNRANKED'}</span>
          </div>
          <button onClick={logout} style={s.sSairBtn}>SAIR</button>
        </aside>

        <main style={s.main}>
          {/* LINHA 1: Agente (Clicável) + Resumo (Clicável) */}
          <div style={s.l1}>
            
            {/* Card Agente Mais Usado */}
            <div 
              style={{ ...s.cardAgente, cursor: 'pointer', transition: 'all 0.3s ease' }}
              onClick={() => navigate('/detalhes-agente', { state: { dados, historico, totalKills, totalAssists } })}
              onMouseOver={(e) => {
                e.currentTarget.style.transform = 'translateY(-5px)';
                e.currentTarget.style.borderColor = '#00e5ff66';
                e.currentTarget.style.boxShadow = '0 10px 25px #00e5ff1a';
              }}
              onMouseOut={(e) => {
                e.currentTarget.style.transform = 'translateY(0)';
                e.currentTarget.style.borderColor = 'rgba(255, 255, 255, 0.07)';
                e.currentTarget.style.boxShadow = 'none';
              }}
            >
              <div style={s.cAgenteImgBox}>
                {agenteArtwork
                  ? <img src={agenteArtwork} alt={dados?.melhorAgente} style={s.cAgenteImg} loading="lazy" />
                  : <div style={s.cAgenteImgVazio} />
                }
              </div>
              <div style={s.cAgenteInfo}>
                <p style={s.cAgenteLabel}>AGENT MAIS USADO</p>
                <p style={s.cAgenteNome}>{dados?.melhorAgente || '—'}</p>
                <p style={s.cAgenteSub}>Taxa de Vitórias | {dados?.taxaVitoria || '0%'}</p>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', marginTop: '0.75rem' }}>
                  {habilidades.length > 0
                    ? habilidades.map((hab, i) => {
                        const statsHab = [
                          Math.round((totalKills / (habilidades.length || 1)) * 10) / 10,
                          Math.round((totalAssists / (habilidades.length || 1)) * 10) / 10,
                          Math.round(((totalKills + totalAssists) / (habilidades.length || 1)) * 10) / 10,
                          Math.round((dados?.kdaGeral || 0) * 10) / 10,
                        ];
                        const val = statsHab[i] || 0;
                        const maxVal = Math.max(...statsHab, 1);
                        return (
                          <div key={i} style={s.habRow}>
                            <div style={s.habIconBox}>
                              <img src={hab.img} alt={hab.nome} style={s.habIconImg} loading="lazy"
                                onError={e => e.target.style.display = 'none'} />
                            </div>
                            <div style={s.habBarFundo}>
                              <div style={{ ...s.habBarPreen, width: `${Math.min((val / maxVal) * 100, 100)}%` }} />
                            </div>
                            <span style={s.habVal}>{val}</span>
                          </div>
                        );
                      })
                    : [0, 1, 2, 3].map(i => (
                      <div key={i} style={s.habRow}>
                        <div style={s.habIconVazio} />
                        <div style={s.habBarFundo} />
                        <span style={s.habVal}>—</span>
                      </div>
                    ))
                  }
                </div>
              </div>
            </div>

            {/* Card Resumo de Desempenho */}
            <div 
              style={{ ...s.cardResumo, cursor: 'pointer', transition: 'all 0.3s ease' }}
              onClick={() => navigate('/detalhes-desempenho', { state: { dados, historico, totalKills, totalAssists, avgACS } })}
              onMouseOver={(e) => {
                e.currentTarget.style.transform = 'translateY(-5px)';
                e.currentTarget.style.borderColor = '#00e5ff66';
                e.currentTarget.style.boxShadow = '0 10px 25px #00e5ff1a';
              }}
              onMouseOut={(e) => {
                e.currentTarget.style.transform = 'translateY(0)';
                e.currentTarget.style.borderColor = 'rgba(255, 255, 255, 0.07)';
                e.currentTarget.style.boxShadow = 'none';
              }}
            >
              <p style={s.secLabel}>RESUMO DE DESEMPENHO</p>
              <div style={s.rTopo}>
                <div style={s.rStat}>
                  <img src={skullIcon} alt="skull" style={s.rIcone} loading="lazy" />
                  <div style={s.rNums}>
                    <span style={s.rNum}>{dados?.kdaGeral || '—'}</span>
                    <span style={s.rLbl}>KDR</span>
                  </div>
                </div>
                <div style={s.rDivider} />
                <div style={s.rStat}>
                  <svg width="44" height="44" viewBox="0 0 44 44" fill="none" xmlns="http://www.w3.org/2000/svg" style={{ opacity: 0.85 }}>
                    <circle cx="22" cy="22" r="14" stroke="white" strokeWidth="2.5"/>
                    <circle cx="22" cy="22" r="4" stroke="white" strokeWidth="2.5"/>
                    <line x1="22" y1="4" x2="22" y2="10" stroke="white" strokeWidth="2.5" strokeLinecap="round"/>
                    <line x1="22" y1="34" x2="22" y2="40" stroke="white" strokeWidth="2.5" strokeLinecap="round"/>
                    <line x1="4" y1="22" x2="10" y2="22" stroke="white" strokeWidth="2.5" strokeLinecap="round"/>
                    <line x1="34" y1="22" x2="40" y2="22" stroke="white" strokeWidth="2.5" strokeLinecap="round"/>
                  </svg>
                  <div style={s.rNums}>
                    <span style={s.rNum}>{avgACS || '—'}</span>
                    <span style={s.rLbl}>ACS</span>
                  </div>
                </div>
              </div>
              <div style={s.rGrid}>
                {[
                  { label: 'ASSISTS',      val: totalAssists },
                  { label: 'FIRST BLOODS', val: '—' },
                  { label: 'HEADSHOT',     val: '—' },
                  { label: 'KILLS',        val: totalKills },
                ].map(({ label, val }) => (
                  <div key={label} style={s.rCell}>
                    <span style={s.rCellLbl}>{label}</span>
                    <span style={s.rCellVal}>{val}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* LINHA 2: Partidas + Gráfico */}
          <div style={s.l2}>
          {/* Card de Últimas Partidas Transformado em Botão Analítico */}
          <div 
            style={{ ...s.cardPartidas, cursor: 'pointer', transition: 'all 0.3s ease' }}
            onClick={() => navigate('/detalhes-partidas', { state: { dados, historico, partidasFiltradas } })}
            onMouseOver={(e) => {
              e.currentTarget.style.transform = 'translateY(-5px)';
              e.currentTarget.style.borderColor = '#00e5ff66'; // Brilho perolado sutil
              e.currentTarget.style.boxShadow = '0 10px 25px rgba(236, 232, 225, 0.05)';
            }}
            onMouseOut={(e) => {
              e.currentTarget.style.transform = 'translateY(0)';
              e.currentTarget.style.borderColor = '#00e5ff1a';
              e.currentTarget.style.boxShadow = 'none';
            }}
          >
            <div style={s.pHeader}>
              <p style={s.secLabel}>ÚLTIMAS PARTIDAS</p>
            </div>
            <div style={s.pList}>
              {partidasFiltradas.slice(0, 5).map((p, i) => {
                const vitoria = p.resultado === 'Vitoria';
                const mapaImg = getMapaImg(p.mapa);
                const agIcon  = getAgenteIcone(p.agente);
                return (
                  <div key={i} style={{ ...s.pRow, borderLeftColor: vitoria ? '#00ff87' : '#ff4655' }}>
                    {/* Imagem do Agente */}
                    {agIcon
                      ? <img src={agIcon} alt={p.agente} style={s.pAgIcon} loading="lazy" />
                      : <div style={s.pAgVazio}>{p.agente?.[0]}</div>
                    }
                    
                    {/* Nome do Mapa e Imagem */}
                    <div style={s.pMapaContainer}>
                      <span style={s.pMapaNome}>{p.mapa?.toUpperCase()}</span>
                      {mapaImg && <img src={mapaImg} alt={p.mapa} style={s.pMapaImg} loading="lazy" />}
                    </div>

                    {/* Bloco de Resultado Sólido (Mais estreito) */}
                    <div style={{ ...s.pTag, background: vitoria ? '#127e5a' : '#ad2f41' }}>
                      <span style={{ color: vitoria ? '#4ff2a9' : '#ff7a8a', fontWeight: 900, fontSize: '0.85rem', letterSpacing: 1.5 }}>
                        {vitoria ? 'VITÓRIA' : 'DERROTA'}
                      </span>
                    </div>

                    {/* Bloco corrigido e formatado: ACS + Kills + Botão Deletar */}
                    <div style={{ flex: 1, display: 'flex', justifyContent: 'flex-end', alignItems: 'center', paddingRight: '1.25rem', gap: '1rem', flexShrink: 0 }}>
                      <span style={{ color:'#ece8e1', fontSize:'0.9rem', fontWeight:800, whiteSpace:'nowrap', letterSpacing: '0.5px' }}>
                        ACS: {Math.round(p.score / 20)} | KILL: {p.kills}
                      </span>
                      
                      <button 
                        onClick={(e) => deletarPartida(e, p.id)}
                        title="Deletar partida"
                        style={{
                          background: 'rgba(255, 70, 85, 0.15)',
                          border: '1px solid rgba(255, 70, 85, 0.3)',
                          color: '#ff4655',
                          borderRadius: '4px',
                          cursor: 'pointer',
                          padding: '0.35rem 0.5rem',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          transition: 'all 0.2s ease'
                        }}
                        onMouseOver={(e) => e.currentTarget.style.background = 'rgba(255, 70, 85, 0.3)'}
                        onMouseOut={(e) => e.currentTarget.style.background = 'rgba(255, 70, 85, 0.15)'}
                      >
                        <svg width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                        </svg>
                      </button>
                    </div>

                  </div>
                );
              })}
            </div>
          </div>

            {/* Card Evolução de Rank Transformado em Botão Analítico */}
            <div 
              style={{ ...s.cardGrafico, cursor: 'pointer', transition: 'all 0.3s ease' }}
              onClick={() => navigate('/detalhes-rank', { state: { dados, historico } })}
              onMouseOver={(e) => {
                e.currentTarget.style.transform = 'translateY(-5px)';
                e.currentTarget.style.borderColor = '#00e5ff66'; // Brilho verde Valorant
                e.currentTarget.style.boxShadow = '0 10px 25px #00e5ff1a';
              }}
              onMouseOut={(e) => {
                e.currentTarget.style.transform = 'translateY(0)';
                e.currentTarget.style.borderColor = 'rgba(255, 255, 255, 0.07)';
                e.currentTarget.style.boxShadow = 'none';
              }}
            >
              <p style={{ ...s.secLabel, textAlign: 'center', marginBottom: '1.5rem' }}>EVOLUÇÃO DO RANK</p>
               {/* Nova div em volta do gráfico para forçá-lo a respeitar o limite de altura */}
              <div style={{ flex: 1, minHeight: 0 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={dadosGrafico}>
                    <defs>
                      <linearGradient id="gkda" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%"  stopColor="#ff4655" stopOpacity={0.5} />
                        <stop offset="95%" stopColor="#ff4655" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <XAxis dataKey="name" hide />
                    <YAxis hide />
                    <Tooltip
                      contentStyle={{ background: '#161b22', border: '1px solid #ff4655', borderRadius: 4, fontSize: '0.72rem' }}
                      formatter={v => [v, 'KDA']}
                    />
                    <Area type="monotone" dataKey="kda" stroke="#ff4655" strokeWidth={2} fill="url(#gkda)" />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
            </div>
          </div>

          {/* LINHA 3: Melhores Mapas */}
          {/* Card Melhores Mapas Transformado em Botão Analítico */}
          <div 
            style={{ ...s.cardMapas, cursor: 'pointer', transition: 'all 0.3s ease' }}
            onClick={() => navigate('/detalhes-mapas', { state: { dados, historico, estatisticas } })}
            onMouseOver={(e) => {
              e.currentTarget.style.transform = 'translateY(-5px)';
              e.currentTarget.style.borderColor = '#00e5ff66'; // Brilho ciano combinando com as barras de progresso
              e.currentTarget.style.boxShadow = '0 10px 25px #00e5ff1a';
            }}
            onMouseOut={(e) => {
              e.currentTarget.style.transform = 'translateY(0)';
              e.currentTarget.style.borderColor = 'rgba(255, 255, 255, 0.07)';
              e.currentTarget.style.boxShadow = 'none';
            }}
          >
            <p style={s.secLabel}>MELHORES MAPAS</p>
            {melhoresMapas.map((m, i) => {
              const mapaImg = getMapaImg(m.mapa);
              const pct = parseFloat(m.taxaVitoria) || 0;
              const corBarra = pct >= 60 ? '#00ff87' : pct >= 40 ? '#00e5ff' : '#ff4655';
              return (
                <div key={i} style={s.mRow}>
                  {mapaImg && <img src={mapaImg} alt={m.mapa} style={s.mImg} loading="lazy" />}
                  <span style={s.mNome}>{m.mapa?.toUpperCase()}</span>
                  <div style={s.mBarFundo}>
                    <div style={{ ...s.mBarPreen, width: `${pct}%`, background: `linear-gradient(90deg, ${corBarra}aa, ${corBarra})` }} />
                    <span style={s.mWinInline}>WIN {m.taxaVitoria}</span>
                  </div>
                </div>
              );
            })}
          </div>
        </main>
      </div>
    </div>
  );
}

const s = {
  page:        { display:'flex', flexDirection:'column', height:'100vh', width:'100vw', color:'#fff', fontFamily:"'Segoe UI',sans-serif", overflow:'hidden', position:'relative', background:'transparent' },
  loadingPage: { display:'flex', flexDirection:'column', justifyContent:'center', alignItems:'center', height:'100vh', background:'#0a0d14' },
  btnRetry:    { padding:'0.5rem 1.5rem', background:'#ff4655', color:'#fff', border:'none', borderRadius:3, cursor:'pointer', fontWeight:700 },
  bgLayer:     { position:'absolute', inset:0, background:`url(${bgAgents})`, backgroundSize:'cover', backgroundPosition:'center', zIndex:0, pointerEvents:'none' },
  contentWrapper: { display:'flex', flex:1, position:'relative', zIndex:1, minHeight: 0 },

  sidebar:     { width:'240px', minWidth:'240px', background:'linear-gradient(180deg,#0d1117 0%,#080c14 100%)', borderRight:'1px solid rgba(255,70,85,0.08)', display:'flex', flexDirection:'column', alignItems:'center', padding:'1.25rem 1rem 1.5rem', gap:'0.5rem', overflow:'hidden', position:'relative', zIndex:1 },
  sTop:        { display:'flex', flexDirection:'column', alignItems:'center', gap:'0.5rem', width:'100%' },
  sLogo:       { width:56, filter:'drop-shadow(0 0 10px rgba(255,70,85,0.9))' },
  sTemporada:  { display:'flex', flexDirection:'column', alignItems:'center', color:'#ffffff', fontSize:'0.82rem', fontWeight:700, letterSpacing:1.5, textAlign:'center', lineHeight:1.8 },
  sAgenteCard: { width:'240px', position:'absolute', top:'320px', bottom:'0', left:'0', right:'0', overflow:'hidden', border:'none', background:'transparent', flexShrink:0 },
  sAgenteImg:  { width:'100%', height:'100%', objectFit:'cover', objectPosition:'center top', maskImage:'linear-gradient(to bottom, rgba(0,0,0,1) 0%, rgba(0,0,0,1) 55%, rgba(0,0,0,0) 100%)', WebkitMaskImage:'linear-gradient(to bottom, rgba(0,0,0,1) 0%, rgba(0,0,0,1) 55%, rgba(0,0,0,0) 100%)' },
  sAgenteVazio:{ width:'100%', height:'100%', display:'flex', alignItems:'center', justifyContent:'center', color:'#374151', fontSize:'2rem', background:'#0f1520' },
  sRankBox:    { display:'flex', flexDirection:'column', alignItems:'center', gap:'0.2rem', position:'relative', zIndex:2, marginTop:'auto' },
  sRankImg:    { width:100 },
  sRankNome:   { color:'#ffffff', fontSize:'0.72rem', fontWeight:900, letterSpacing:3 },
  sSairBtn:    { padding:'0.4rem 1.25rem', background:'transparent', border:'1px solid rgb(255, 70, 86)', color:'#ffffff', borderRadius:2, cursor:'pointer', fontSize:'0.72rem', fontWeight:700, letterSpacing:2, marginTop:'0.5rem', position:'relative', zIndex:2 },

  main: { flex:1, overflowY:'auto', padding:'0 1.25rem 1rem', display:'flex', flexDirection:'column', gap:'0.75rem', position:'relative', zIndex:1 },

  topbar:      { display:'flex', justifyContent:'space-between', alignItems:'center', borderBottom:'1px solid rgba(255,70,85,0.08)', padding:'1.125rem 1.25rem', background:'linear-gradient(180deg,#0d1117 0%,#080c14 100%)', width:'100%', position:'relative', zIndex:1 },
  filterContainer: { display:'inline-flex', background:'rgba(10, 13, 20, 0.6)', padding:'0.4rem', borderRadius:'8px', border:'1px solid rgba(255, 255, 255, 0.05)', backdropFilter:'blur(10px)', gap:'0.5rem' },
  btnFiltro:       { background:'transparent', border:'none', color:'#8b978f', padding:'0.6rem 1.5rem', borderRadius:'6px', cursor:'pointer', fontWeight:'700', letterSpacing:'1px', transition:'all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1)', fontSize:'0.85rem' },
  btnFiltroAtivo:  { background:'#ece8e1', color:'#0a0d14', boxShadow:'0 4px 15px rgba(255,255,255,0.1)' },

  topbarRight: { display:'flex', alignItems:'center', gap:'2rem' },
  playerTag:   { display:'flex', alignItems:'center', gap:'0.5rem' },
  playerNome:  { color:'#ffffff', fontSize:'0.95rem', fontWeight:700 },

  secLabel: { color:'#ffffff', fontSize:'0.8rem', fontWeight:700, letterSpacing:2, textTransform:'uppercase', marginBottom:'0.75rem' },

  l1: { display:'flex', gap:'0.75rem', flexWrap: 'wrap' },

  cardAgente:     { flex:1.2, background:'rgba(10,14,22,0.98)', border:'1px solid rgba(255,255,255,0.07)', borderRadius:3, display:'flex', overflow:'hidden', minHeight:'190px' },
  cAgenteImgBox:  { width:'130px', minWidth:'130px', flexShrink:0, overflow:'hidden', background:'#0a0f1a', borderRight:'1px solid rgba(255,255,255,0.04)' },
  cAgenteImg:     { width:'100%', height:'100%', objectFit:'cover', objectPosition:'center top' },
  cAgenteImgVazio:{ width:'100%', height:'100%', background:'#0a0f1a' },
  cAgenteInfo:    { flex:1, padding:'0.875rem 1rem', display:'flex', flexDirection:'column' },
  cAgenteLabel:   { color:'#ffffff', fontSize:'1.00rem', fontWeight:700, letterSpacing:2, textTransform:'uppercase', marginBottom:'0.25rem' },
  cAgenteNome:    { color:'#fff', fontSize:'1.4rem', fontWeight:900, marginBottom:'0.2rem' },
  cAgenteSub:     { color:'#6b7280', fontSize:'0.88rem', marginBottom:'0.1rem' },
  habRow:         { display:'flex', alignItems:'center', gap:'0.6rem' },
  habIconBox:     { width:24, height:24, flexShrink:0, display:'flex', alignItems:'center', justifyContent:'center' },
  habIconImg:     { width:'100%', height:'100%', objectFit:'contain' },
  habIconVazio:   { width:24, height:24, background:'rgba(255,255,255,0.04)', borderRadius:2 },
  habBarFundo:    { flex:1, height:9, background:'rgba(255,255,255,0.05)', borderRadius:10, overflow:'hidden' },
  habBarPreen:    { height:'100%', background:'linear-gradient(90deg,#00ff87,#00e5ff)', borderRadius:10 },
  habVal:         { color:'#6b7280', fontSize:'0.82rem', minWidth:36, textAlign:'right' },

  cardResumo: { flex:1, background:'rgba(10,14,22,0.98)', border:'1px solid rgba(255,255,255,0.07)', borderRadius:3, padding:'0.875rem 1.125rem' },
  rTopo:      { display:'flex', alignItems:'center', gap:'1.75rem', marginBottom:'0.875rem', paddingBottom:'0.875rem', borderBottom:'1px solid rgba(255,255,255,0.05)' },
  rStat:      { display:'flex', alignItems:'center', gap:'0.75rem' },
  rIcone:     { width:80, height:80, objectFit:'contain', opacity:0.85 },
  rDivider:   { width:1, height:54, background:'rgba(255,70,85,0.12)', flexShrink:0 },
  rNums:      { display:'flex', flexDirection:'column' },
  rNum:       { color:'#ff4655', fontSize:'2.8rem', fontWeight:900, lineHeight:1 },
  rLbl:       { color:'#4b5563', fontSize:'0.78rem', fontWeight:700, letterSpacing:2, marginTop:'0.2rem' },
  rGrid:      { display:'grid', gridTemplateColumns:'1fr 1fr', gap:'0.35rem' },
  rCell:      { background:'rgba(255,255,255,0.025)', borderRadius:3, padding:'0.4rem 0.75rem', textAlign:'center' },
  rCellLbl:   { display:'block', color:'#4b5563', fontSize:'0.7rem', fontWeight:700, letterSpacing:1.5, marginBottom:'0.2rem' },
  rCellVal:   { display:'block', color:'#e5e7eb', fontSize:'1.1rem', fontWeight:900 },

  l2: { display:'flex', gap:'0.75rem', flexWrap: 'wrap' },

  // Card Partidas - Estética Blocada e Proporcional (Figma Prototype)
  cardPartidas: { flex:2, background:'rgba(10,14,22,0.98)', border:'1px solid rgba(255,255,255,0.07)', borderRadius:3, padding:0, display:'flex', flexDirection:'column', overflow:'hidden' },
  pHeader:  { padding: '0.875rem 1.25rem 0.5rem', margin: 0, borderBottom: '1px solid rgba(255,255,255,0.04)' },
  pList:    { display: 'flex', flexDirection: 'column' },
  // Altura reduzida para 48px para ficar mais "sleek"
  pRow:     { display:'flex', alignItems:'stretch', height:'48px', background:'rgba(255,255,255,0.01)', borderBottom:'1px solid rgba(255,255,255,0.03)', borderLeft:'3px solid transparent' },
  pAgIcon:  { width:48, height:'100%', objectFit:'cover', flexShrink:0 },
  pAgVazio: { width:48, height:'100%', background:'#0f1520', display:'flex', alignItems:'center', justifyContent:'center', color:'#374151', fontSize:'1rem', fontWeight: 900, flexShrink:0 },
  // Fontes menores e elementos mais justos
  pMapaContainer: { flex: 1, position: 'relative', display: 'flex', alignItems: 'center', paddingLeft: '1.25rem', overflow: 'hidden' },
  pMapaNome:{ fontSize:'1.1rem', fontWeight:900, letterSpacing:1.5, color: '#ece8e1', position: 'relative', zIndex: 2 },
  pMapaImg: { position: 'absolute', right: 0, top: 0, height: '100%', width: '130px', objectFit: 'cover', maskImage: 'linear-gradient(to right, transparent 0%, black 80%)', WebkitMaskImage: 'linear-gradient(to right, transparent 0%, black 80%)', opacity: 0.5, zIndex: 1 },
  // Largura do bloco de resultado diminuída
  pTag:     { width: '110px', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink:0 },
  pStats:   { flex: 1, display: 'flex', justifyContent: 'flex-end', alignItems: 'center', paddingRight: '1.25rem', color:'#ece8e1', fontSize:'0.9rem', fontWeight:800, whiteSpace:'nowrap', flexShrink:0, letterSpacing: '0.5px' },

  cardGrafico: { flex:1, background:'rgba(10,14,22,0.98)', border:'1px solid rgba(255,255,255,0.07)', borderRadius:3, padding:'0.875rem 1rem', display:'flex', flexDirection:'column' },

  cardMapas: { background:'rgba(10,14,22,0.98)', border:'1px solid rgba(255,255,255,0.07)', borderRadius:3, padding:'0.875rem 1rem' },
  mRow:      { display:'flex', alignItems:'center', gap:'0.75rem', padding:'0.3rem 0' },
  mImg:      { width:60, height:36, objectFit:'cover', borderRadius:2, flexShrink:0 },
  mNome:     { fontSize:'0.88rem', fontWeight:700, letterSpacing:1.5, minWidth:75, flexShrink:0 },
  mBarFundo: { flex:1, height:28, background:'rgba(255,255,255,0.04)', borderRadius:3, overflow:'hidden', position:'relative', display:'flex', alignItems:'center' },
  mBarPreen: { position:'absolute', left:0, top:0, bottom:0, borderRadius:3 },
  mWinInline:{ position:'relative', zIndex:1, color:'#fff', fontSize:'0.88rem', fontWeight:700, letterSpacing:1, paddingLeft:'0.875rem' },
};