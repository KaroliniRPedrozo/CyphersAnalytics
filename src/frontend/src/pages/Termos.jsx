import PaginaLegal, { H2, P, UL } from '../components/PaginaLegal';
import { AVISO_RIOT } from '../components/Rodape';

export default function Termos() {
  return (
    <PaginaLegal titulo="Termos de Uso" atualizado="novembro de 2026 (versão 2026-11)">
      <P>Ao criar uma conta no Cypher's Analytics você concorda com estes termos e com a Política de Privacidade.</P>

      <H2>1. O serviço</H2>
      <P>Plataforma gratuita que exibe estatísticas dos seus próprios jogos de VALORANT. Os dados vêm da Riot Games (ou de provedores intermediários) e podem ter atraso, lacunas ou imprecisões.</P>

      <H2>2. Sua conta</H2>
      <UL>
        <li>Você declara ter a idade mínima exigida pela Riot Games para jogar VALORANT e fornecer informações verdadeiras.</li>
        <li>Você é responsável por manter sua senha em sigilo e deve informar apenas um Riot ID que seja seu.</li>
        <li>Quando disponível, o vínculo da conta Riot (Riot Sign On) confirma que o Riot ID é seu.</li>
      </UL>

      <H2>3. Uso permitido</H2>
      <P>É proibido: usar o serviço para obter vantagem injusta no jogo ou para consultar dados de outros jogadores; tentar burlar limites, autenticação ou segurança; automatizar acessos em massa; ou usar o serviço de forma contrária à lei ou às políticas da Riot Games.</P>

      <H2>4. Disponibilidade</H2>
      <P>O serviço é oferecido "como está", sem garantia de disponibilidade contínua. Podemos limitar, suspender ou encerrar funcionalidades, inclusive por mudanças ou limites impostos pela API da Riot.</P>

      <H2>5. Encerramento</H2>
      <P>Você pode excluir sua conta a qualquer momento em "Minha conta". Podemos suspender contas que violem estes termos.</P>

      <H2>6. Propriedade intelectual e Riot Games</H2>
      <P>{AVISO_RIOT}</P>

      <H2>7. Alterações e foro</H2>
      <P>Podemos atualizar estes termos; mudanças relevantes serão comunicadas e exigirão novo aceite. Fica eleito o foro do domicílio do usuário, conforme a legislação brasileira aplicável.</P>

      <H2>8. Contato</H2>
      <P><strong>[EMAIL_DE_CONTATO]</strong></P>
    </PaginaLegal>
  );
}
