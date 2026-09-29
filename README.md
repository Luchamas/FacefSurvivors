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
- **7 armas** (8 níveis cada): Caneta, Lápis, Régua, Livros, Café, Relâmpago e HP 12C.
- **9 itens passivos**: Vitamina C, Tênis de Corrida, Energético, Agenda, Óculos, Ímã, Marmita, Moletom e Xerox.
- **Inimigos**: estudantes! Calouro, Atrasado, Sonolento, Veterano, Nerd e Repetente, com ondas que mudam
  a cada minuto, além de super ondas (correrias e cercos) anunciadas com o sinal e o aviso **Hora do Intervalo!**
- **Chefes**: a *Prova Surpresa* aparece aos 5:00 e deixa cair um baú. *O TCC* aparece aos 10:00 e é a luta final.
- **Coletáveis**: gemas de XP, coxinha (recupera vida), ímã (puxa todas as gemas) e baú (3 melhorias grátis).
- Tela de fim de jogo com tempo, nível, abates e dano causado por cada arma.

Imagens importadas (em `Assets/Resources`):

| Arquivo | Uso |
|---|---|
| `UI/Logo.png` | Logo do menu |
| `Sprites/Mascot_Idle.png`, `Sprites/Mascot_Run.png` | Quadros do personagem (mesmo tamanho, pés alinhados, desenho olhando para a direita) |
| `Sprites/Floor.png` | Piso: 3 x 2 ladrilhos, repetível |
| `Sprites/Table.png` | Mesa com 6 cadeiras vista de cima (com sombra) |

Os estudantes, armas e itens são pixel art gerada por código, e os sons (8-bit) também são sintetizados em código.

## Estrutura do código

```
Assets/Scripts/
  Core/       GameSettings (opções salvas), AudioManager (sons sintetizados), Art e PixelArt (gráficos),
              UI (fábrica de interface), Bootstrap
  Data/       Definitions (classes de dados) e Database (personagens, armas, passivos, inimigos)
  Game/       GameController (estados da partida), Player, EnemyManager (ondas, chefes, colisão),
              Weapons, Projectiles, PickupManager, Effects, HUD, Upgrades, World (câmera e piso),
              Obstacles (mesas: layout, colisão e desvio)
  Menu/       MainMenuController e OptionsPanel
  Editor/     ProjectSetup (cria cenas e configura o Build)
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
- **Desenhos**: em `Assets/Scripts/Core/Art.cs` cada sprite é um desenho em texto (um caractere por pixel).
- **Nova arma**: crie uma classe que herda de `Weapon` (ou `BurstWeapon`) em `Weapons.cs`
  e adicione um `WeaponDef` na lista `Database.Weapons`.
