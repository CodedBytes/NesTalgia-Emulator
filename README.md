## NesTalgia Emulator
Um emulador feito com o propósito de estudar os comportamentos da uma CPU de NES, estudos sobre emulação e preservação de hardware de forma fiél, entre outros propósitos como estudo sobre assuntos relacionados a dados em Bitwise e outras técnicas que possam envolver codigo de baixo nivel de alguma forma.
O emulador foi feito em uma liguagem de alto nivel como CSharp, mas o assunto "Emulação" em questão, me trouxe muito conteudo sobre linguagem de baixo nivel e linguagem de maquina.

## Compilação
Eu não irei disponibilizar arquivos já compilados do emulador na aba release, pois o intuito deste projeto não é disponibilizar algo pronto, mas sim incentivar a criação para novos programadores.
O projeto é disponibilizado da forma que esta, e a compilação é de total responsabilidade do cloner.

Em caso de compilação:
- você pode realizar a instalação do SDL-2 pelo VS 2022 ou a versão mais recente para seu computador.
- A necessidade do framework do .NET é do 8.0 para cima, então tenha certeza de ter estes requisitos.

Apos realizar a instalação do SDL-2, você só precisa compilar o projeto, é bem simples!

## Status do Emulador
Meu emulador está em um nivel bem inicial, se comparado com alguns projetos ja presentes no github. O mesmo foi organizado de uma forma diferente e totalmente comentado para que todos, de qualquer nivel de programação, possa entender o que está acontecendo com o codigo apresentado neste repositório.
Neste repositório estou utilizando a linguagem CSharp para realizar a emulação deste console de videogame, o Nintendo Entertainment System (NES), com uma forma de organização de arquivos que seja clara para todos nos níveis de conhecimento.
Estou utilizando também o SDL para realizar a renderização providenciada pela PPU do emulador, mas com o tempo, devo substituir o mesmo por uma solução mais simples e que não tenha tanto impacto na emulação.

## Funcionalidades
As funcionalidades do emulador ainda se mantem no básico, mas vou lista-las abaixo:
- PPU: Funcional, mas ainda não consegue renderizar sprites de Foreground, apenas background.
- CPU: Toda a CPU foi baseada no 6502 original, com alguns tratamentos para OPS e instruções ilegais do Ricoh 2A03.
- BUS: A BUS da CPU (MemoryMap) está bem completa, já pronta para testes de gameplay, talvez necessitando de alguns ajustes.
- Controle: A classe de controle esta completa, mas ainda preciso realizar testes depois da PPU para testes.
- Emulação de cartucho - Mappers: Apenas o mapper 0 foi implementado, a medida que o emulador ficar com a PPU em seu estado completo, pretendo implementar mappers para aumentar a compatibilidade do emulador. 
- APU: Não implementada.

## Licensa
A lincensa do projeto esta em GPLv3.
Peço que leiam com atenção as cláusulas da licensa vigente para não cometer infrações ao codigo ao clonar o repositório para seu uso pessoal.
