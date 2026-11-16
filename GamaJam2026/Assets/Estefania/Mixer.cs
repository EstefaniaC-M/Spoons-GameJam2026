using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public struct Recipe
{
    public ChemicalType result;
    public ChemicalType ingredientA;
    public ChemicalType ingredientB;
}

public class Mixer : MonoBehaviour
{
    public static Mixer Instance;

    public TMP_Text messageText;   // optional: shows "Wrong ingredient!" and "You win!"
    public float mixDelay = 1f;    // how long the two layers show before they merge

    private Recipe[] recipes;
    private Dictionary<Tube, List<ChemicalType>> poured = new Dictionary<Tube, List<ChemicalType>>();
    private HashSet<Tube> finished = new HashSet<Tube>();
    private int mixed = 0;

    void Awake()
    {
        Instance = this;

        recipes = new Recipe[]
        {
            new Recipe { result = ChemicalType.Purple, ingredientA = ChemicalType.Red,  ingredientB = ChemicalType.Blue },
            new Recipe { result = ChemicalType.Green,  ingredientA = ChemicalType.Blue, ingredientB = ChemicalType.Yellow }
        };

        Say("");
    }

    void Say(string s)
    {
        if (messageText != null) messageText.text = s;
    }

    Color Col(ChemicalType t)
    {
        return ChemicalInfo.Get(t).color;
    }

    public void Pour(Tube ingredient, Tube target)
    {
        if (ingredient.isResultTube || !target.isResultTube) return;
        if (finished.Contains(target)) return;

        foreach (Recipe r in recipes)
        {
            if (r.result != target.chemical) continue;

            if (ingredient.chemical != r.ingredientA && ingredient.chemical != r.ingredientB)
            {
                Say("Wrong ingredient!");
                return;
            }

            if (!poured.ContainsKey(target))
                poured[target] = new List<ChemicalType>();

            List<ChemicalType> list = poured[target];

            if (list.Contains(ingredient.chemical))
            {
                Say("Already added that one");
                return;
            }

            list.Add(ingredient.chemical);
            Say("");

            if (list.Count == 1)
            {
                target.ShowLayers(Col(list[0]), Color.clear, false);
            }
            else
            {
                // blue sinks to the bottom, otherwise the first one poured stays on the bottom
                ChemicalType bottom = list.Contains(ChemicalType.Blue) ? ChemicalType.Blue : list[0];
                ChemicalType top = (bottom == list[0]) ? list[1] : list[0];

                target.ShowLayers(Col(bottom), Col(top), true);
                finished.Add(target);
                StartCoroutine(FinishMix(target));
            }
            return;
        }
    }

    IEnumerator FinishMix(Tube target)
    {
        yield return new WaitForSeconds(mixDelay);

        target.ShowFilled();
        mixed++;

        if (mixed >= recipes.Length)
            Say("You win!");
    }

    // optional: hook this to a Reset button
    public void ResetPuzzle()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}