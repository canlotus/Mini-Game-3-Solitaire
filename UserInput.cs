using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UserInput : MonoBehaviour
{
    public GameObject slot1;
    private Solitaire solitaire;
    private float timer;
    private float doubleClickTime = 0.3f;
    private int clickCount = 0;

    void Start()
    {
        solitaire = FindObjectOfType<Solitaire>();
        slot1 = this.gameObject;
    }

    void Update()
    {
        if (clickCount == 1)
            timer += Time.deltaTime;

        if (clickCount == 3)
        {
            timer = 0;
            clickCount = 1;
        }

        if (timer > doubleClickTime)
        {
            timer = 0;
            clickCount = 0;
        }

        GetMouseClick();
    }

    void GetMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickCount++;
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, -10));

            RaycastHit2D hit = Physics2D.Raycast(
                Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit)
            {
                if (hit.collider.CompareTag("Deck"))
                {
                    Deck();
                }
                else if (hit.collider.CompareTag("Card"))
                {
                    Card(hit.collider.gameObject);
                }
                else if (hit.collider.CompareTag("Top"))
                {
                    Top(hit.collider.gameObject);
                }
                else if (hit.collider.CompareTag("Bottom"))
                {
                    Bottom(hit.collider.gameObject);
                }
            }
        }
    }

    void Deck()
    {
        print("Deck");
        solitaire.DealFromDeck();
    }

    void Card(GameObject selected)
    {
        print("card");
        if (slot1 == null) slot1 = this.gameObject;

        Selectable selectedCard = selected.GetComponent<Selectable>();

        // Eğer kartın parent'ı deckButton değilse, artık deck'ten gelmiyor demektir.
        if (selected.transform.parent != solitaire.deckButton.transform)
        {
            selectedCard.inDeckPile = false;
        }

        // Kart kapalıysa ve engelli değilse açalım
        if (!selectedCard.faceUp)
        {
            if (!Blocked(selected))
            {
                selectedCard.faceUp = true;
                slot1 = this.gameObject;
            }
        }
        // Kart desteden (inDeckPile) geliyorsa
        else if (selectedCard.inDeckPile)
        {
            if (!Blocked(selected))
            {
                // Aynı karta tekrar tıklama (double-click) kontrolü
                if (slot1 == selected)
                {
                    if (DoubleClick())
                    {
                        AutoStack(selected);
                        return;
                    }
                }
                else
                {
                    slot1 = selected;
                }
            }
        }
        // Masa (bottom) vb. açık kartlar
        else
        {
            // Çift tıklama kontrolü
            if (slot1 == selected)
            {
                if (DoubleClick())
                {
                    AutoStack(selected);
                    return;
                }
            }

            // Slot boşsa, seçilen kartı slot1'e al
            if (slot1 == this.gameObject)
            {
                slot1 = selected;
            }
            // Slot doluysa ve farklı kartsa stack deneyelim
            else if (slot1 != selected)
            {
                if (Stackable(selected))
                {
                    Stack(selected);
                }
                else
                {
                    slot1 = selected;
                }
            }
        }
    }

    void Top(GameObject selected)
    {
        print("top");
        if (slot1 != null && slot1.CompareTag("Card"))
        {
            Stack(selected);
        }
    }

    void Bottom(GameObject selected)
    {
        print("bottom");
        if (slot1 != null && slot1.CompareTag("Card"))
        {
            // Sadece King (13) boş bir yığına gidebilir gibi bir kural varsa
            if (slot1.GetComponent<Selectable>().value == 13)
            {
                Stack(selected);
            }
        }
    }

    /// <summary>
    /// Bu metot, slot1 (s1) kartının, tıklanan (s2) karta yığılıp yığılmayacağını kontrol eder.
    /// Top (foundation) ve bottom (tableau) için ayrı kurallar var.
    /// </summary>
    bool Stackable(GameObject selected)
    {
        if (slot1 == null)
        {
            slot1 = this.gameObject;
            return false;
        }
        Selectable s1 = slot1.GetComponent<Selectable>();
        Selectable s2 = selected.GetComponent<Selectable>();

        // "Gerçekten top mu?" kontrolü:
        bool isS2Top = s2.top;

        // Eğer s2.top false ama parent'i "Top" tag'li bir placeholder ise yine top sayalım
        if (!isS2Top && s2.transform.parent != null && s2.transform.parent.CompareTag("Top"))
        {
            isS2Top = true;
        }

        // Eğer hedef top alanı ise (foundation)
        if (isS2Top)
        {
            // Kural: Aynı suit + (alt kartın değeri = üst kartın değeri + 1)
            // Örneğin 2, A'nın üstüne gidecekse (value=2, A'nın value=1) => 2 == 1+1
            // veya A'yı boş bir top'a koymak (foundation) vs.
            if (s1.suit == s2.suit || (s1.value == 1 && s2.suit == null))
            {
                if (s1.value == s2.value + 1)
                {
                    return true;
                }
            }
            return false;
        }
        else
        {
            // Masa (tableau) yığın kuralı: Renk zıt + (alt kart = üst kart - 1)
            // Örneğin 6 (siyah) 7 (kırmızı)'nın üstüne gidebilir
            if (s1.value == s2.value - 1)
            {
                bool card1Red = (s1.suit != "C" && s1.suit != "S");
                bool card2Red = (s2.suit != "C" && s2.suit != "S");

                // Aynı renkse stack olmaz
                if (card1Red == card2Red)
                {
                    print("Non Stackable");
                    return false;
                }
                else
                {
                    print("Stackable");
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// slot1 kartını, parametre olarak gelen selected (s2) kartının üstüne koyar.
    /// </summary>
    void Stack(GameObject selected)
    {
        Selectable s1 = slot1.GetComponent<Selectable>();
        Selectable s2 = selected.GetComponent<Selectable>();

        // Foundation (top) ise ofset 0, tableau ise 0.35f
        float yOffset = 0.35f;
        if (s2.top || (!s2.top && s1.value == 13))
            yOffset = 0;

        slot1.transform.position = new Vector3(
            selected.transform.position.x,
            selected.transform.position.y - yOffset,
            selected.transform.position.z - 0.01f);

        // Parent-child relationship
        slot1.transform.parent = selected.transform;

        // Deck'ten geliyorsa tripsOnDisplay listesinden çıkar
        if (s1.inDeckPile)
        {
            solitaire.tripsOnDsiplay.Remove(slot1.name);
        }
        // Top'tan top'a As koyma gibi durum varsa
        else if (s1.top && s2.top && s1.value == 1)
        {
            solitaire.topPos[s1.row].GetComponent<Selectable>().value = 0;
            solitaire.topPos[s1.row].GetComponent<Selectable>().suit = null;
        }
        // Başka bir foundation'a taşıma
        else if (s1.top)
        {
            solitaire.topPos[s1.row].GetComponent<Selectable>().value = s1.value - 1;
        }
        else
        {
            // Masa yığınından kartı çıkar
            solitaire.bottoms[s1.row].Remove(slot1.name);
        }

        // Artık deck'ten değil
        s1.inDeckPile = false;

        // s1'in yeni row'u, s2'nin row'u olsun
        s1.row = s2.row;

        // s2 top ise, s1 de top oluyor
        if (s2.top)
        {
            solitaire.topPos[s1.row].GetComponent<Selectable>().value = s1.value;
            solitaire.topPos[s1.row].GetComponent<Selectable>().suit = s1.suit;
            s1.top = true;  // Ek: s1 de top
        }
        else
        {
            s1.top = false;
        }

        // Seçimi sıfırla
        slot1 = this.gameObject;
    }

    /// <summary>
    /// Kartın engellenip engellenmediğini (örneğin altında başka kart varsa) kontrol ediyor.
    /// </summary>
    bool Blocked(GameObject selected)
    {
        Selectable s2 = selected.GetComponent<Selectable>();

        if (s2.inDeckPile)
        {
            // Desteden gelen kartlar
            if (solitaire.tripsOnDsiplay.Count == 0) return false;

            string lastName = solitaire.tripsOnDsiplay.Last();
            GameObject lastObj = GameObject.Find(lastName);

            if (lastObj == null)
            {
                solitaire.tripsOnDsiplay.RemoveAt(solitaire.tripsOnDsiplay.Count - 1);
                return false;
            }

            if (s2.name == lastName)
                return false;
            else
            {
                print(s2.name + " is blocked by " + lastName);
                return true;
            }
        }
        else
        {
            // Masa (tableau) kartları
            if (solitaire.bottoms[s2.row].Count == 0)
                return false;

            if (s2.name == solitaire.bottoms[s2.row].Last())
                return false;
            else
                return true;
        }
    }

    bool DoubleClick()
    {
        if (timer < doubleClickTime && clickCount == 2)
        {
            print("Double click");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Çift tıklama ile kartı otomatik foundation'a (top) taşıma.
    /// HasNoChildren kontrolünü kaldırdık veya esnettik.
    /// </summary>
    void AutoStack(GameObject selected)
    {
        Selectable s1 = selected.GetComponent<Selectable>();

        for (int i = 0; i < solitaire.topPos.Length; i++)
        {
            Selectable stack = solitaire.topPos[i].GetComponent<Selectable>();

            // As ise boş top'a gidebilir
            if (s1.value == 1)
            {
                if (stack.value == 0)
                {
                    slot1 = selected;
                    Stack(solitaire.topPos[i].gameObject);
                    break;
                }
            }
            else
            {
                // Suit aynı, value = top'taki + 1
                if (stack.suit == s1.suit && stack.value == s1.value - 1)
                {
                    // HasNoChildren kontrolünü tamamen kaldırıyoruz
                    slot1 = selected;

                    // Son kart ismini oluştur
                    string lastCardname = stack.suit + stack.value.ToString();
                    if (stack.value == 1) lastCardname = stack.suit + "A";
                    if (stack.value == 11) lastCardname = stack.suit + "J";
                    if (stack.value == 12) lastCardname = stack.suit + "Q";
                    if (stack.value == 13) lastCardname = stack.suit + "K";

                    GameObject lastCard = GameObject.Find(lastCardname);
                    if (lastCard != null)
                    {
                        Stack(lastCard);
                    }
                    break;
                }
            }
        }
    }

    bool HasNoChildren(GameObject card)
    {
        // Eğer altındaki kartlar taşınmaya engel oluyorsa, bu metodu kullanabilirsiniz.
        // Ancak AutoStack'te engel olmaması için orada çağırmayabilirsiniz.
        int childCount = 0;
        foreach (Transform child in card.transform)
            childCount++;
        return childCount == 0;
    }
}