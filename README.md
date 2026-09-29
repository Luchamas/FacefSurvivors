# FACEF Survivors

Jogo 2D de sobrevivência no estilo **Vampire Survivors**, feito em Unity.
Você controla o Mascote FACEF cercado por ondas de estudantes: as armas atacam sozinhas,
você só se movimenta, coleta gemas de XP e escolhe melhorias a cada nível.
Sobreviva até **10:00** e derrote **o TCC** para ser **APROVADO**.

## Como abrir

1. Abra o **Unity Hub** → **Add** → **Add project from disk** → selecione esta pasta.
2. Use o Unity **6000.5.10f1** (Unity 6).
3. Na primeira abertura o projeto cria sozinho as cenas `MainMenu` e `Game`, adiciona as duas ao Build e abre o menu.
   Se precisar refazer isso, use o menu **FACEF Survivors → Configurar projeto** no editor.
4. Aperte **Play**.

Para gerar o executável: **File → Build Profiles → Windows → Build**.

## Controles

| Ação | Teclado | Controle |
|---|---|---|
| Mover | WASD ou setas | Analógico esquerdo |
| Navegar nos menus | Setas + Enter, ou mouse | Analógico + A |
| Escolher melhoria | 1, 2, 3 (ou setas + Enter) | Analógico + A |
| Pausar | Esc ou P | Start |
| Voltar | Esc | B |

## O que tem no jogo

- **Menu principal**: Jogar, Opções, Créditos e Sair, com fundo animado.
- **Opções**: volume geral, música e efeitos, tela cheia, resolução, VSync, números de dano,
  tremor de tela e contador de FPS. Tudo fica salvo e as mesmas opções aparecem no menu de pausa.
- **Personagem**: o Mascote FACEF, com animação de corrida (2 quadros) e respiração quando parado.
- **Cenário**: piso de ladrilhos brancos infinito, com mesas redondas espalhadas que bloqueiam
  o jogador e os inimigos (os inimigos contornam as mesas). O layout muda a cada partida.
- **7 armas** (8 níveis cada), cada uma uma linguagem de programação: TypeScript, Dart, Bash, Lua, HTML/CSS, SQL e C.
- **9 itens passivos**, também linguagens: PHP, Swift, C#, Go, JavaScript, Python, Java, Rust e C++.
- **Inimigos**: estudantes! Calouro, Atrasado, Sonolento, Veterano, Nerd e Repetente, com ondas que mudam
  a cada minuto, além de super ondas (correrias e cercos) anunciadas com o sinal e o aviso **Hora do Intervalo!**
- **Chefes**: a *Prova Surpresa* aparece aos 5:00 e deixa cair um baú. *O TCC* aparece aos 10:00 e é a luta final.
- **Coletáveis**: gemas de XP, coxinha (recupera vida), ímã (puxa todas as gemas) e baú (3 melhorias grátis).
- Tela de fim de jogo com tempo, nível, abates e dano causado por cada arma.

## Imagens e sons

Todas as imagens são arquivos prontos em `Assets/Resources`; nenhum gráfico é desenhado por código.

| Arquivo / pasta | Uso |
|---|---|
| `UI/Logo.png` | Logo do menu |
| `UI/Kenney/` | Interface: painéis, botões (normal, destacado e pressionado), slots, barras (XP, vida, chefe e sliders), alça dos sliders e vinheta (fundo do menu e aviso de dano) |
| `Sprites/Mascot_Idle.png`, `Sprites/Mascot_Run.png` | Quadros do personagem (mesmo tamanho, pés alinhados, desenho olhando para a direita) |
| `Sprites/Floor.png` | Piso: 3 x 2 ladrilhos, repetível |
| `Sprites/Table.png` | Mesa com 6 cadeiras vista de cima (com sombra) |
| `Sprites/Students/<personagem>/` | Quadros dos estudantes (`walk0`–`walk7` e `run0`–`run2`, olhando para a direita): `MaleAdventurer` (Calouro), `FemaleAdventurer` (Atrasado, correndo), `Zombie` (Sonolento), `FemalePerson` (Veterano), `Robot` (Nerd) e `MalePerson` (Repetente) |
| `Sprites/Bosses/` | `Prova.png` (Prova Surpresa) e `TCC.png` (O TCC) |
| `Sprites/Languages/` | Logos das linguagens: ícones das armas e passivos, e projéteis de TypeScript, Dart, C e Lua |
| `Sprites/Items/` | Gemas de XP (`GemBlue`, `GemGreen` e `GemRed`, conforme o valor), baú (`Chest`) e ímã (`Magnet`) |
| `Sprites/Pixel/Coxinha.png` | Coxinha. Tudo em `Sprites/Pixel/` é importado sem suavização (pixel art) |
| `Sprites/Effects/` | Partículas (`Dot`), aura do HTML/CSS (`Disc`), anéis de impacto (`Ring`), sombras (`Shadow`), golpe do Bash (`Slash`) e raios do SQL (`Lightning0` e `Lightning1`) |
| `Audio/Soundtrack.mp3` | Música, tocada em loop |

Imagens novas em `Sprites/` ou `UI/` e músicas em `Audio/` são configuradas sozinhas ao serem importadas
(`ProjectSetup.cs`). Para trocar uma imagem, substitua o PNG mantendo o mesmo nome.

Os efeitos sonoros (estilo 8-bit) são sintetizados em código, em `AudioManager.cs`.

## Estrutura do código

```
Assets/Scripts/
  Core/       GameSettings (opções salvas), AudioManager (efeitos sintetizados e música),
              Art (carrega as imagens e define tamanho, pivô e 9-slice), UI (fábrica de interface), Bootstrap
  Data/       Definitions (classes de dados) e Database (personagens, armas, passivos, inimigos)
  Game/       GameController (estados da partida), Player, EnemyManager (ondas, chefes, colisão),
              Weapons, Projectiles, PickupManager, Effects, HUD, Upgrades, World (câmera e piso),
              Obstacles (mesas: layout, colisão e desvio)
  Menu/       MainMenuController e OptionsPanel
  Editor/     ProjectSetup (cria cenas, configura o Build e a importação de imagens e música)
```

Tudo é montado por código a partir de dois objetos nas cenas: `MainMenu` (com `MainMenuController`)
e `Game` (com `GameController`). Não há prefabs para configurar.

## Como modificar

- **Balanceamento** (dano, vida, velocidade, níveis): `Assets/Scripts/Data/Database.cs`.
- **Ondas de inimigos e super ondas (Hora do Intervalo)**: `BuildWaves` e `BuildEvents` em `Assets/Scripts/Game/EnemyManager.cs`.
- **Duração da partida / chefe final**: `GameController.FinalBossTime`.
- **Personagem** (nome, arma inicial, bônus, imagens): lista `Database.Characters`.
  Tamanho e velocidade da animação: constantes no topo de `Player.cs`.
- **Mesas** (espaçamento, raio de colisão, quantidade): constantes no topo de `Assets/Scripts/Game/Obstacles.cs`.
- **Tamanho dos ladrilhos do piso**: `GroundTiler.TileWorld` em `World.cs`.
- **Imagens**: os arquivos ficam em `Assets/Resources` (veja a tabela acima); o tamanho no mundo, o pivô
  e as bordas 9-slice de cada uma são definidos em `Assets/Scripts/Core/Art.cs`.
- **Nova arma**: crie uma classe que herda de `Weapon` (ou `BurstWeapon`) em `Weapons.cs`
  e adicione um `WeaponDef` na lista `Database.Weapons`, com o logo da linguagem em `Sprites/Languages/`
  como ícone (`Icon = () => Art.Language("Nome")`).
