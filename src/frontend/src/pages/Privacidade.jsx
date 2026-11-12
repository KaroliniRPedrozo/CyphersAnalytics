import PaginaLegal, { H2, P, UL } from '../components/PaginaLegal';

export default function Privacidade() {
  return (
    <PaginaLegal titulo="Política de Privacidade" atualizado="novembro de 2026 (versão 2026-11)">
      <P>
        Esta política explica quais dados o Cypher's Analytics coleta, por quê, com quem compartilha e como você
        exerce seus direitos, conforme a Lei Geral de Proteção de Dados (LGPD, Lei nº 13.709/2018).
      </P>

      <H2>1. Quem é o controlador</H2>
      <P>[NOME DO RESPONSÁVEL] — contato e encarregado de dados: <strong>[EMAIL_DE_CONTATO]</strong>.</P>

      <H2>2. Dados que coletamos</H2>
      <UL>
        <li><strong>Conta:</strong> e-mail e senha. A senha é guardada somente na forma de hash (BCrypt); nunca em texto.</li>
        <li><strong>Conta de jogo:</strong> Riot ID (nome e tag), identificador PUUID e rank atual.</li>
        <li><strong>Partidas:</strong> mapa, modo, agente, resultado, abates/mortes/assistências, pontuação e data.</li>
        <li><strong>Consentimento:</strong> data e versão dos termos aceitos e, quando disponível, a data do vínculo com a conta Riot (Riot Sign On).</li>
      </UL>

      <H2>3. Para que usamos</H2>
      <P>Exclusivamente para mostrar a você mesmo suas estatísticas e evolução. Não usamos seus dados para publicidade, não os vendemos e não os exibimos a outros usuários.</P>

      <H2>4. Base legal</H2>
      <P>Consentimento (LGPD, art. 7º, I), dado no cadastro ou na página "Minha conta" e revogável a qualquer momento.</P>

      <H2>5. Com quem compartilhamos</H2>
      <UL>
        <li><strong>Riot Games:</strong> consultamos a API da Riot para obter os dados de partidas e a conta vinculada. Ao vincular sua conta pelo Riot Sign On, você autoriza o acesso aos dados da sua conta nos limites do escopo solicitado, e entende que o vínculo torna seus dados de partida acessíveis ao Cypher's Analytics.</li>
        <li><strong>Provedor de dados de terceiros:</strong> enquanto a integração oficial não estiver ativa, dados de partida podem ser consultados por meio de um serviço de terceiros (HenrikDev), usando seu Riot ID.</li>
        <li><strong>Hospedagem e banco de dados:</strong> provedores de infraestrutura que armazenam a aplicação e os dados, sob contrato e apenas para esse fim.</li>
      </UL>

      <H2>6. Por quanto tempo guardamos</H2>
      <P>Enquanto sua conta existir. Se você revogar o consentimento, apagamos o histórico de partidas e o cache de jogador. Se excluir a conta, removemos definitivamente todos os dados.</P>

      <H2>7. Seus direitos</H2>
      <P>Na página "Minha conta" você pode, sem precisar falar com ninguém: baixar uma cópia dos seus dados, revogar o consentimento e excluir a conta. Também pode pedir confirmação de tratamento, correção ou informações adicionais pelo e-mail acima.</P>

      <H2>8. Segurança</H2>
      <UL>
        <li>Conexões protegidas por HTTPS e senhas com hash.</li>
        <li>Limite de tentativas de login e de requisições para dificultar abuso.</li>
        <li>Cada usuário só acessa os próprios dados.</li>
      </UL>

      <H2>9. Armazenamento no navegador</H2>
      <P>Guardamos no seu navegador apenas o token de sessão e dados temporários de exibição. Não usamos cookies de rastreamento nem ferramentas de publicidade.</P>

      <H2>10. Alterações</H2>
      <P>Se esta política mudar de forma relevante, pediremos um novo consentimento.</P>
    </PaginaLegal>
  );
}
