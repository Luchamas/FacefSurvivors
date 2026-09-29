using UnityEngine;

namespace FacefSurvivors
{
    /// <summary>
    /// Um inimigo em jogo. É uma classe simples (não MonoBehaviour): o
    /// <see cref="EnemyManager"/> atualiza todos de uma vez, o que é bem mais rápido
    /// quando há centenas deles na tela.
    /// </summary>
    public class Enemy
    {
        public EnemyDef Def;
        public GameObject Go;
        public Transform Tr;
        public SpriteRenderer Sr;
        public Sprite[] Frames;    // animação de andar (null = imagem parada)

        public Vector2 Pos;
        public Vector2 Knock;
        public Vector2 FixedDir;   // != zero: anda em linha reta (revoadas)
        public float Life;         // tempo de vida para inimigos de revoada

        public float Hp, MaxHp, Speed, Damage, Radius, Mass;
        public bool Alive;
        public int Index;          // posição na lista de ativos
        public float Flash;
        public float AnimOffset;
        public readonly float[] HitTimes = new float[8];

        // chefes
        public float ChargeTimer, ChargeTime, Telegraph, SummonTimer;
        public Vector2 ChargeDir;

        public void ResetHitTimes()
        {
            for (int i = 0; i < HitTimes.Length; i++) HitTimes[i] = -999f;
        }
    }
}
