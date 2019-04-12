using BepInEx;
using RoR2;
using UnityEngine;
using System;
using MonoMod.Cil;

namespace DifficultyModifier
{
    [BepInPlugin("com.elzheiz.difficultymodifier", "DifficultyModifier", "1.0")]

    public class DifficultyModifier : BaseUnityPlugin
    {
        private int difficultyIncrementIndex = 0;
        private float[] difficultyIncrements = { 1.0f, 10.0f, 60.0f, 600.0f, 3600.0f };
        private float totalDifficultyIncrement;

        private bool difficultyPaused = false;

        public void Awake()
        {
            // This should be replaced by a proper IL Hook to replace this.fixedTime inside the method itself
            // for improved mod compatibiliy, otherwise fixedTime will be wrong inside that method.
            On.RoR2.Run.OnFixedUpdate += (orig, self) =>
            {
                float savedFixedTime = self.fixedTime;

                // Increment fixedTime and then put it back after the method has been used.
                self.fixedTime += totalDifficultyIncrement;
                orig(self);
                self.fixedTime = savedFixedTime;
            };

            On.RoR2.Run.OnDestroy += (orig, self) =>
            {
                totalDifficultyIncrement = 0;
                orig(self);
            };
        }

        public void Update()
        {
            // Exit if we're not in a run.
            if (!Run.instance) { return; }

            // Otherwise control time using the + - * / buttons
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                // I -> Inspect the current difficuly increment and coefficients.
                if (Input.GetKeyDown(KeyCode.I))
                {
                    Debug.Log("compensatedDifficultyCoefficient = " + Run.instance.compensatedDifficultyCoefficient);
                    Debug.Log("difficultyCoefficient = " + Run.instance.difficultyCoefficient);
                    Debug.Log("Total Difficulty Increment = " + totalDifficultyIncrement);
                }

                // P -> Pause the game difficulty.
                if (Input.GetKeyDown(KeyCode.P))
                {
                    difficultyPaused = !difficultyPaused;
                }
                // + -> Increments the timer
                else if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    totalDifficultyIncrement += difficultyIncrements[difficultyIncrementIndex];
                    Debug.Log("Slide difficulty bar by +" + difficultyIncrements[difficultyIncrementIndex] + "s (Additional difficulty is: " + totalDifficultyIncrement + "s)");
                }
                // - -> Decrements the timer as much as possible
                else if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    if (Run.instance.fixedTime + totalDifficultyIncrement - difficultyIncrements[difficultyIncrementIndex] > 0)
                    {
                        totalDifficultyIncrement -= difficultyIncrements[difficultyIncrementIndex];
                        Debug.Log("Slide difficulty bar by -" + difficultyIncrements[difficultyIncrementIndex] + "s (Additional difficulty is: " + totalDifficultyIncrement + "s)");
                    }
                    else
                    {
                        totalDifficultyIncrement = -Run.instance.fixedTime;
                        Debug.Log("Slide difficulty bar by -" + Run.instance.fixedTime + "s (Additional difficulty is: " + totalDifficultyIncrement + "s)");
                    }
                }
                // * -> Increases the timer increment step
                else if (Input.GetKeyDown(KeyCode.KeypadMultiply))
                {
                    if (difficultyIncrementIndex + 1 < difficultyIncrements.Length)
                    {
                        difficultyIncrementIndex++;
                        Debug.Log("Difficulty bar increment is now " + difficultyIncrements[difficultyIncrementIndex] + "s");
                    }
                }
                // / -> Decreases the timer increment step
                else if (Input.GetKeyDown(KeyCode.KeypadDivide))
                {
                    if (difficultyIncrementIndex - 1 >= 0)
                    {
                        difficultyIncrementIndex--;
                        Debug.Log("Difficulty bar increment is now " + difficultyIncrements[difficultyIncrementIndex] + "s");
                    }
                }
            }
        }
    }
}
