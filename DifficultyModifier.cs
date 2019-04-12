using BepInEx;
using RoR2;
using RoR2.UI;
using UnityEngine;
using System;
using System.Text;
using MonoMod.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace DifficultyModifier
{
    [BepInPlugin("com.elzheiz.difficultymodifier", "DifficultyModifier", "1.0")]

    public class DifficultyModifier : BaseUnityPlugin
    {
        private int timeIncrementIndex = 0;
        private float[] timeIncrements = { 1.0f, 10.0f, 60.0f, 600.0f, 3600.0f };
        private float totalIncrement;

        public void Awake()
        {
            IL.RoR2.UI.TimerText.Update += (il) =>
            {
                ILCursor c = new ILCursor(il).Goto(0);
                c.EmitDelegate(() =>
                {
                    if (Run.instance)
                    {
                        Chat.AddMessage("Time: " + (Run.instance.time - totalIncrement));
                    }
                });

                c.GotoNext(x => x.MatchLdloc(1));
                c.GotoNext(x => x.MatchLdloc(1));
                c.EmitDelegate<Func<float>>(() =>
                {
                    return Run.instance.time - totalIncrement;
                });
                c.RemoveRange(2);
            };
        }

        public void Update()
        {
            // Exit if we're not in a run.
            if (!Run.instance) { return; }

            if (Input.GetKeyDown(KeyCode.I))
            {
                Chat.AddMessage("compensatedDifficultyCoefficient = " + Run.instance.compensatedDifficultyCoefficient);
                Chat.AddMessage("difficultyCoefficient = " + Run.instance.difficultyCoefficient);
                Chat.AddMessage("totalIncrement = " + totalIncrement);
            }

            // Otherwise control time using the + - * / buttons
            // + -> Increments the timer
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                totalIncrement += timeIncrements[timeIncrementIndex];
                Run.instance.time += timeIncrements[timeIncrementIndex];
                Run.instance.fixedTime += timeIncrements[timeIncrementIndex];
            }
            // - -> Decrements the timer
            else if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                if (Run.instance.time > timeIncrements[timeIncrementIndex])
                {
                    Run.instance.time -= timeIncrements[timeIncrementIndex];
                }
                else
                {
                    Run.instance.time = 0.0f;
                }

                if (Run.instance.fixedTime > timeIncrements[timeIncrementIndex])
                {
                    totalIncrement -= timeIncrements[timeIncrementIndex];
                    Run.instance.fixedTime -= timeIncrements[timeIncrementIndex];
                }
                else
                {
                    totalIncrement -= Run.instance.fixedTime;
                    Run.instance.fixedTime = 0.0f;
                }
            }
            // * -> Increases the timer increment step
            else if (Input.GetKeyDown(KeyCode.KeypadMultiply))
            {
                if(timeIncrementIndex + 1 < timeIncrements.Length)
                {
                    timeIncrementIndex++;
                    Chat.AddMessage("New time increment: " + timeIncrements[timeIncrementIndex] + "s");
                }
            }
            // / -> Decreases the timer increment step
            else if (Input.GetKeyDown(KeyCode.KeypadDivide))
            {
                if (timeIncrementIndex - 1 >= 0)
                {
                    timeIncrementIndex--;
                    Chat.AddMessage("New time increment: " + timeIncrements[timeIncrementIndex] + "s");
                }
            }
        }
    }
}
