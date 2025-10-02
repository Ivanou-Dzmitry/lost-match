using UnityEngine;

public class SpecialElements : MonoBehaviour
{

    private GoalManager goalManagerClass;
    private GameBoard gameBoardClass;
    private BonusShop bonusShopClass;
    private UIManager uiManagerClass;

    public int hitPoints;

    [Header("Sound")]
    public AudioClip[] elementSounds;

    [Header("Particles")]
    public GameObject[] elementParticles;

    //for mylti hits objects
    [Header("Layers")]
    public GameObject[] elementLayers;

    public bool wasHitThisFrame = false;
    public bool isMatched = false;   

    // Start is called before the first frame update
    void Start()
    {
        //classes
        goalManagerClass = GameObject.FindWithTag("GoalManager").GetComponent<GoalManager>();
        bonusShopClass = GameObject.FindWithTag("BonusShop").GetComponent<BonusShop>();
        gameBoardClass = GameObject.FindWithTag("GameBoard").GetComponent<GameBoard>();
        uiManagerClass = GameObject.FindWithTag("UIManager").GetComponent<UIManager>();
    }

    private void OnMouseDown()
    {
        if(bonusShopClass.bonusSelected == 0)
        {
            UseShuffle();
        }
    }

    private void UseShuffle()
    {
        gameBoardClass.ShuffleBoard();
        //show panel with text
        uiManagerClass.ShowInGameInfo("Mixed up", true, 0, ColorPalette.Colors["DarkBlue"]); 

        bonusShopClass.bonusSelected = -1;
        bonusShopClass.bonusDescPanel.SetActive(false);

        bonusShopClass.shopState = BonusShop.ShopState.Game;
    }

    // Update is called once per frame
    void Update()
    {
        int Column = 0;
        int Row = 0;

        this.wasHitThisFrame = false;

        if (hitPoints <= 0)
        {
            //for goals for breakable
            if (goalManagerClass != null)
            {
                string tagForCompare = this.gameObject.tag;

                Column = (int)this.gameObject.transform.position.x;
                Row = (int)this.gameObject.transform.position.y;

                //hack for various breakable
                if (this.gameObject.tag == "breakable_02" || this.gameObject.tag == "breakable_03" && this.gameObject.tag != null)
                {
                    tagForCompare = "breakable_01";
                }

                //hack for various blockers
                if (this.gameObject.tag == "blocker_02" || this.gameObject.tag == "blocker_03" && this.gameObject.tag != null)
                {
                    tagForCompare = "blocker_01";
                }

                //important
                goalManagerClass.CompareGoal(tagForCompare, Column, Row, true); //call #7

                goalManagerClass.UpdateGoals();
            }

            Destroy(this.gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        hitPoints -= damage;

        LayerManager();
    }

    //show layers of the element (for visual destruction)
    void LayerManager()
    {
        //hide layers
        for (int i = 0; i < elementLayers.Length; i++)
        {
            if (elementLayers[i] != null && hitPoints == i + 1)
            {
                elementLayers[i].gameObject.SetActive(false);
            }
        }
    }
}
