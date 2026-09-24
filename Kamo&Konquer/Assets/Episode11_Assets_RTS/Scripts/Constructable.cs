using UnityEngine;
using UnityEngine.AI;

public class Constructable : MonoBehaviour
{
    [SerializeField] private float maxLeben = 100f;
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteBeschaedigt;

    private float aktuellesLeben;
    private SpriteRenderer sr;
    private NavMeshObstacle obstacle;
    private Spawner spawner;

    void Awake()
    {
        // Sprite-Renderer kann auch in einem Kindobjekt liegen
        sr = GetComponentInChildren<SpriteRenderer>();
        spawner = GetComponent<Spawner>(); // optional, kann auch null sein
        aktuellesLeben = maxLeben;
        SpriteAktualisieren();
    }

    // Wird vom RPGAttackingState aufgerufen
    public void ReceiveDamage(float schaden)
    {
        aktuellesLeben -= schaden;
        aktuellesLeben = Mathf.Max(aktuellesLeben, 0f);
        Debug.Log(aktuellesLeben);

        if (aktuellesLeben <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        SpriteAktualisieren();
    }

    private void SpriteAktualisieren()
    {
        if (sr == null) return;
        sr.sprite = (aktuellesLeben / maxLeben) < 0.5f ? spriteBeschaedigt : spriteNormal;
    }

    public void TakeDamage(int damage)
    {
        aktuellesLeben -= damage;
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        Debug.Log(aktuellesLeben);
    }

    public void ConstructableWasPlaced()
    {
        ActivateObstacle();

        if (spawner != null)
            spawner.StartSpawning();
    }

    private void ActivateObstacle()
    {
        obstacle = GetComponentInChildren<NavMeshObstacle>();
        if (obstacle != null) obstacle.enabled = true;
    }
}