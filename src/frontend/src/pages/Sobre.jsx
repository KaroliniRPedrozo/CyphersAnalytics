import { Link } from 'react-router-dom';
import PaginaLegal, { H2, P, UL } from '../components/PaginaLegal';
import { AVISO_RIOT } from '../components/Rodape';

export default function Sobre() {
  return (
    <PaginaLegal titulo="Sobre o Cypher's Analytics">
      <P>
        O Cypher's Analytics é uma plataforma gratuita para jogadores de VALORANT acompanharem a
        própria evolução: transformamos o histórico de partidas em gráficos e indicadores fáceis de ler,
        para você entender onde está evoluindo e onde precisa treinar.
      </P>

      <H2>O que você encontra aqui</H2>
      <UL>
        <li>Resumo de desempenho: KDA geral, taxa de vitória e total de partidas analisadas.</li>
        <li>Desempenho por agente e por mapa, com destaque para seus pontos fortes e fracos.</li>
        <li>Evolução do KDA ao longo das últimas partidas e histórico detalhado.</li>
        <li>Aviso de agentes desativados temporariamente, vindo do status oficial do jogo.</li>
      </UL>

      <H2>Seus dados, sob o seu controle</H2>
      <P>
        Você só vê os seus próprios dados, e apenas depois de autorizar o uso deles. Nenhum outro usuário
        consegue consultar o seu perfil. Você pode baixar tudo o que guardamos, revogar a autorização ou
        excluir a conta a qualquer momento na página "Minha conta". Detalhes na{' '}
        <Link to="/privacidade" style={{ color: '#ff4655' }}>Política de Privacidade</Link>.
      </P>

      <H2>O que não fazemos</H2>
      <UL>
        <li>Não criamos ranking ou "MMR" próprio: mostramos apenas o rank oficial do jogo.</li>
        <li>Não oferecemos dados em tempo real de partidas em andamento nem ferramentas para scouting de adversários.</li>
        <li>Não vendemos dados e não exibimos anúncios.</li>
      </UL>

      <H2>Quem faz</H2>
      <P>
        Projeto desenvolvido por Karolini R. Pedrozo. Contato: <strong>[EMAIL_DE_CONTATO]</strong>.
      </P>

      <H2>Aviso legal</H2>
      <P>{AVISO_RIOT}</P>
    </PaginaLegal>
  );
}
