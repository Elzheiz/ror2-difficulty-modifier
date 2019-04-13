using BepInEx;
using RoR2;
using UnityEngine;
using System;
using MonoMod.Cil;

namespace DifficultyModifier
{
    [BepInPlugin("com.elzheiz.difficultymodifier", "DifficultyModifier", "1.1.0")]

    public class DifficultyModifier : BaseUnityPlugin
    {
        // Control the difficulty in steps
        private int difficultyIncrementIndex = 0;
        private float[] difficultyIncrements = { 1.0f, 10.0f, 60.0f, 600.0f, 3600.0f };
        private float totalDifficultyIncrement = 0.0f;

        // Pause the difficulty and snapshot the various coefficients
        private bool pausedDifficulty = false;
        private float pausedDifficultyCoefficient = 0.0f;
        private float pausedCompensatedDifficultyCoefficient = 0.0f;
        private float pausedTargetMonsterLevel = 0.0f;

        // Compensate the various coefficients after unpausing to avoid a jump in difficulty
        private float offsetDifficultyCoefficentJump = 0.0f;
        private float offsetCompensatedDifficultyCoefficentJump = 0.0f;
        private float offsetTargetMonsterLevelJump = 0.0f;


        public void Awake()
        {
            IL.RoR2.Run.OnFixedUpdate += (il) =>
            {
                ILCursor c = new ILCursor(il).Goto(0);
                // Get to the next fixedTime load, go to the previous instruction (which should be "this")
                // Then add the new instruction and remove "this.fixedTime"
                // DANGEROUS if we leave fixedTime behind -> Infinite loop
                while (c.Goto(0).TryGotoNext(x => x.MatchLdfld<Run>("fixedTime")))
                {
                    c.GotoPrev();
                    c.EmitDelegate<Func<float>>(() =>
                    {
                        if (!Run.instance) { return 0.0f; }

                        return Run.instance.fixedTime + totalDifficultyIncrement;
                    });
                    c.RemoveRange(2);
                }

                // Pause the compensatedDifficultyCoefficient or add the jump offset to it
                c.Goto(0).TryGotoNext(MoveType.After, x => x.MatchStfld<Run>("compensatedDifficultyCoefficient"));
                c.EmitDelegate<Action>(() =>
                {
                    if (!Run.instance) { return; }
                    if (pausedDifficulty)
                    {
                        // Since we replace the coefficient after it's been calculated,
                        // Run.instance.compensatedDifficultyCoefficient contains the actual value the coefficient should be, which we can then compare to the paused one
                        offsetCompensatedDifficultyCoefficentJump = Run.instance.compensatedDifficultyCoefficient - pausedCompensatedDifficultyCoefficient;
                        Run.instance.compensatedDifficultyCoefficient = pausedCompensatedDifficultyCoefficient;
                    }
                    else
                    {
                        // If unpaused, add the jump offset to the coefficient to avoid a difficulty jump.
                        Run.instance.compensatedDifficultyCoefficient -= offsetCompensatedDifficultyCoefficentJump;
                    }
                });

                // Pause the difficultyCoefficient or add the jump offset to it
                c.Goto(0).TryGotoNext(MoveType.After, x => x.MatchStfld<Run>("difficultyCoefficient"));
                c.EmitDelegate<Action>(() =>
                {
                    if (!Run.instance) { return; }
                    if (pausedDifficulty)
                    {
                        offsetDifficultyCoefficentJump = Run.instance.difficultyCoefficient - pausedDifficultyCoefficient;
                        Run.instance.difficultyCoefficient = pausedDifficultyCoefficient;
                    }
                    else
                    {
                        Run.instance.difficultyCoefficient -= offsetDifficultyCoefficentJump;
                    }
                });
                
                // Now pause the targetMonsterLevel or add the jump offset to it
                c.Goto(0).TryGotoNext(MoveType.After, x => x.MatchCallvirt<Run>("set_targetMonsterLevel"));
                c.EmitDelegate<Action>(() =>
                {
                    if (!Run.instance) { return; }
                    if (pausedDifficulty)
                    {
                        offsetTargetMonsterLevelJump = Run.instance.targetMonsterLevel - pausedTargetMonsterLevel;
                        typeof(Run).GetProperty("targetMonsterLevel").SetValue(Run.instance, pausedTargetMonsterLevel);
                    }
                    else
                    {
                        typeof(Run).GetProperty("targetMonsterLevel").SetValue(Run.instance, Run.instance.targetMonsterLevel - offsetTargetMonsterLevelJump);
                    }
                });
            };

            // Reset the increment when the run is terminated.
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
                // I -> Inspect the current difficulty increment and coefficients.
                if (Input.GetKeyDown(KeyCode.I))
                {
                    Debug.Log("compensatedDifficultyCoefficient = " + Run.instance.compensatedDifficultyCoefficient + "\n" +
                        "difficultyCoefficient = " + Run.instance.difficultyCoefficient + "\n" +
                        "targetMonsterLevel = " + Run.instance.targetMonsterLevel + "\n" +
                        "offsetDifficultyJump = " + offsetDifficultyCoefficentJump + "\n" +
                        "offsetCompensatedDifficultyCoefficentJump = " + offsetCompensatedDifficultyCoefficentJump + "\n" +
                        "offsetTargetMonsterLevelJump = " + offsetTargetMonsterLevelJump + "\n" +
                        "Total Difficulty Increment = " + totalDifficultyIncrement + "\n"
                    );
                }
                // P -> Pause/Unpause the difficulty
                else if (Input.GetKeyDown(KeyCode.P))
                {
                    pausedDifficulty = !pausedDifficulty;

                    if (pausedDifficulty)
                    {
                        // Save the values when it's been paused
                        pausedDifficultyCoefficient = Run.instance.difficultyCoefficient;
                        pausedCompensatedDifficultyCoefficient = Run.instance.compensatedDifficultyCoefficient;
                        pausedTargetMonsterLevel = Run.instance.targetMonsterLevel;
                        Debug.Log("Difficulty paused.");
                    }
                    else
                    {
                        Debug.Log("Difficulty unpaused.");
                    }
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
