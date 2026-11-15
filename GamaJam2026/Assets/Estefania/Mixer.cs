using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

    private Recipe[] recipes;
    private Dictionary<Tube, HashSet<ChemicalType>> poured = new Dictionary<Tube, HashSet<ChemicalType>>();
    private HashSet<Tube> finished = new HashSet<Tube>();

    void Awake()
    {
        Instance = this;

        recipes = new Recipe[]
        {
            new Recipe { result = ChemicalType.Purple, ingredientA = ChemicalType.Red,  ingredientB = ChemicalType.Blue },
            new Recipe { result = ChemicalType.Green,  ingredientA = ChemicalType.Blue, ingredientB = ChemicalType.Yellow }
        };
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
                Debug.Log("Wrong ingredient for " + target.chemical);
                return;
            }

            if (!poured.ContainsKey(target))
                poured[target] = new HashSet<ChemicalType>();

            poured[target].Add(ingredient.chemical);

            // the tube takes on the color of what was just poured in
            target.ShowColor(ChemicalInfo.Get(ingredient.chemical).color);

            if (poured[target].Contains(r.ingredientA) && poured[target].Contains(r.ingredientB))
            {
                target.ShowFilled();   // both ingredients in, so it turns purple or green
                finished.Add(target);

                if (finished.Count >= recipes.Length)
                    Debug.Log("You win!");   // put your win screen here
            }
            return;
        }
    }
}