using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CasinoSystem
{
    public class SlotMachineUI : Casino
    {
        [Header("Slot Machine Specific")]
        [SerializeField] private TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[15];
        [SerializeField] private TextMeshProUGUI jackpotText;
        [SerializeField] private float spinDuration = 1.5f;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private int jackpotIncrement = 10;
        [SerializeField] private float highlightDelay = 1.0f;

        [Header("Developer Cheats")]
        public bool forceHighMultiplierWin = false;

        private int jackpot = 1000;
        private readonly string[] symbols = { "‡", "†", "‰", "♣", "♦", "♥", "♠" };
        private readonly int[] symbolMultipliers = { 1, 2, 3, 4, 5, 6, 7 };
        private readonly float[] chanceToShow = { 19.4f, 19.4f, 14.9f, 14.9f, 11.9f, 11.9f, 7.6f };

        private List<string>[] reels;

        private class Pattern
        {
            public string Name;
            public float Multiplier;
            public int[][] Lines;
        }

        private List<Pattern> patterns;

        private struct WinningPatternInfo
        {
            public Pattern PatternData;
            public int[] LineIndices;
            public int WinAmount;
            public Color HighlightColor;
            public float FinalMultiplier;
        }

        protected override void Start()
        {
            base.Start();
            InitPatterns();
            UpdateJackpotText();

            if (symbols.Length != chanceToShow.Length || symbols.Length != symbolMultipliers.Length)
            {
                Debug.LogError("Liczba symboli, ich mnożników i szans na wystąpienie musi być taka sama!");
            }
        }

        private int GetRandomSymbolIndexBasedOnChance()
        {
            float totalChance = chanceToShow.Sum();
            float randomValue = Random.Range(0, totalChance);
            float cumulativeChance = 0f;

            for (int i = 0; i < chanceToShow.Length; i++)
            {
                cumulativeChance += chanceToShow[i];
                if (randomValue < cumulativeChance)
                {
                    return i;
                }
            }
            return chanceToShow.Length - 1;
        }

        private void InitPatterns()
        {
            patterns = new List<Pattern>
            {
                new Pattern { Name = "HORIZONTAL", Multiplier = 1.0f, Lines = new int[][]
                    {
                        new[]{0,1,2}, new[]{1,2,3}, new[]{2,3,4},
                        new[]{5,6,7}, new[]{6,7,8}, new[]{7,8,9},
                        new[]{10,11,12}, new[]{11,12,13}, new[]{12,13,14}
                    }
                },
                new Pattern { Name = "VERTICAL", Multiplier = 1.0f, Lines = new int[][]
                    {
                        new[]{0,5,10}, new[]{1,6,11}, new[]{2,7,12}, new[]{3,8,13}, new[]{4,9,14}
                    }
                },
                new Pattern { Name = "DIAGONAL", Multiplier = 1.0f, Lines = new int[][]
                    {
                        new[]{0,6,12}, new[]{1,7,13}, new[]{2,8,14}, new[]{10,6,2}, new[]{11,7,3}, new[]{12,8,4}
                    }
                },
                new Pattern { Name = "HORIZONTAL-L", Multiplier = 2.0f, Lines = new int[][]
                    {
                        new[]{0,1,2,3}, new[]{1,2,3,4},
                        new[]{5,6,7,8}, new[]{6,7,8,9},
                        new[]{10,11,12,13}, new[]{11,12,13,14}
                    }
                },
                new Pattern { Name = "HORIZONTAL-XL", Multiplier = 3.0f, Lines = new int[][]
                    {
                        new[]{0,1,2,3,4}, new[]{5,6,7,8,9}, new[]{10,11,12,13,14}
                    }
                },
                new Pattern { Name = "ZIG", Multiplier = 4.0f, Lines = new int[][] { new[]{2, 6, 8, 10, 14} } },
                new Pattern { Name = "ZAG", Multiplier = 4.0f, Lines = new int[][] { new[]{0, 4, 6, 8, 12} } },
                new Pattern { Name = "ABOVE", Multiplier = 7.0f, Lines = new int[][] { new[]{ 2, 6, 8, 10, 11, 12, 13, 14} } },
                new Pattern { Name = "BELOW", Multiplier = 7.0f, Lines = new int[][] { new[]{0, 1, 2, 3, 4, 6, 8, 12} } },
                new Pattern { Name = "EYE", Multiplier = 8.0f, Lines = new int[][] { new[]{1, 2, 3, 5, 6, 8, 9, 11, 12, 13} } },
                new Pattern { Name = "JACKPOT", Multiplier = 10.0f, Lines = new int[][] { new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14} } }
            };

            patterns = patterns.OrderBy(p => p.Multiplier).ToList();
        }

        public override void PlayGame()
        {
            if (betAmount > PlayerMoney)
            {
                resultText.text = "Nie masz wystarczająco pieniędzy!";
                return;
            }
            isGameInProgress = true;
            DisableGameControls();
            StartCoroutine(SpinSlots(betAmount, true));
        }

        public void OnAutoSpinClicked()
        {
            isGameInProgress = true;
            DisableGameControls();
            StartCoroutine(AutoSpin(100));
        }

        private IEnumerator AutoSpin(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (betAmount > PlayerMoney)
                {
                    resultText.text = "Brak środków na dalsze spiny!";
                    break;
                }
                yield return StartCoroutine(SpinSlots(betAmount, false));
                yield return new WaitForSeconds(0.5f);
            }
            isGameInProgress = false;
            EnableGameControls();
        }

        private IEnumerator SpinSlots(int bet, bool endSpinOnFinish)
        {
            foreach (var t in slotTexts) t.color = Color.white;
            resultText.text = "";

            reels = new List<string>[5];
            for (int c = 0; c < 5; c++)
            {
                reels[c] = new List<string>();
                for (int i = 0; i < 30; i++)
                    reels[c].Add(symbols[GetRandomSymbolIndexBasedOnChance()]);
            }

            string[] plannedFinal = null;
            if (forceHighMultiplierWin)
            {
                var candidates = patterns.Where(p => p.Multiplier >= 7.0f).ToList();
                if (candidates.Count > 0)
                {
                    var patternToForce = candidates[Random.Range(0, candidates.Count)];
                    var lineToForce = patternToForce.Lines[Random.Range(0, patternToForce.Lines.Length)];
                    var winningSymbol = symbols[Random.Range(0, symbols.Length)];

                    plannedFinal = new string[15];
                    for (int i = 0; i < 15; i++)
                    {
                        string s;
                        do { s = symbols[GetRandomSymbolIndexBasedOnChance()]; } while (s == winningSymbol);
                        plannedFinal[i] = s;
                    }
                    foreach (var idx in lineToForce) plannedFinal[idx] = winningSymbol;
                }
            }

            float elapsed = 0f;
            float stepTime = 0.075f;
            bool[] columnStopped = new bool[5];
            float[] stopTimes = new float[5];
            int[] currentOffsets = new int[5];

            for (int c = 0; c < 5; c++)
            {
                stopTimes[c] = spinDuration + c * 0.8f;
                int count = reels[c].Count;
                int start = Random.Range(0, count);
                slotTexts[0 * 5 + c].text = reels[c][start % count];
                slotTexts[1 * 5 + c].text = reels[c][(start + 1) % count];
                slotTexts[2 * 5 + c].text = reels[c][(start + 2) % count];
                currentOffsets[c] = (start + 3) % count;
            }

            while (true)
            {
                bool allStopped = true;
                for (int c = 0; c < 5; c++)
                {
                    if (!columnStopped[c])
                    {
                        allStopped = false;

                        string nextSymbol = reels[c][currentOffsets[c]];
                        currentOffsets[c] = (currentOffsets[c] + 1) % reels[c].Count;

                        int top = 0 * 5 + c;
                        int mid = 1 * 5 + c;
                        int bot = 2 * 5 + c;
                        slotTexts[bot].text = slotTexts[mid].text;
                        slotTexts[mid].text = slotTexts[top].text;
                        slotTexts[top].text = nextSymbol;
                    }
                }

                elapsed += stepTime;
                for (int c = 0; c < 5; c++)
                {
                    if (!columnStopped[c] && elapsed >= stopTimes[c])
                    {
                        columnStopped[c] = true;

                        if (plannedFinal != null)
                        {
                            slotTexts[0 * 5 + c].text = plannedFinal[0 * 5 + c];
                            slotTexts[1 * 5 + c].text = plannedFinal[1 * 5 + c];
                            slotTexts[2 * 5 + c].text = plannedFinal[2 * 5 + c];
                        }
                    }
                }

                if (allStopped) break;
                yield return new WaitForSeconds(stepTime);
            }

            PlayerMoney -= bet;

            var allFoundPatterns = new List<WinningPatternInfo>();
            foreach (var pattern in this.patterns)
            {
                foreach (var line in pattern.Lines)
                {
                    string firstSymbol = slotTexts[line[0]].text;
                    if (line.All(index => slotTexts[index].text == firstSymbol))
                    {
                        int symbolIndex = System.Array.IndexOf(symbols, firstSymbol);
                        if (symbolIndex != -1)
                        {
                            int symbolMultiplier = symbolMultipliers[symbolIndex];
                            float finalMultiplier = pattern.Multiplier * symbolMultiplier;
                            int winAmount = Mathf.RoundToInt(bet * finalMultiplier);

                            allFoundPatterns.Add(new WinningPatternInfo
                            {
                                PatternData = pattern,
                                LineIndices = line,
                                WinAmount = winAmount,
                                HighlightColor = GetColorForPattern(pattern.Name),
                                FinalMultiplier = finalMultiplier
                            });
                        }
                    }
                }
            }

            var patternsToRemove = new HashSet<WinningPatternInfo>();
            foreach (var potentialSuperPattern in allFoundPatterns)
            {
                foreach (var potentialSubPattern in allFoundPatterns)
                {
                    if (potentialSuperPattern.Equals(potentialSubPattern)) continue;
                    if (potentialSubPattern.LineIndices.Length < potentialSuperPattern.LineIndices.Length &&
                        potentialSubPattern.PatternData.Multiplier <= potentialSuperPattern.PatternData.Multiplier)
                    {
                        var superPatternIndices = new HashSet<int>(potentialSuperPattern.LineIndices);
                        if (potentialSubPattern.LineIndices.All(index => superPatternIndices.Contains(index)))
                        {
                            patternsToRemove.Add(potentialSubPattern);
                        }
                    }
                }
            }

            var filteredPatterns = allFoundPatterns.Where(p => !patternsToRemove.Contains(p)).ToList();

            if (filteredPatterns.Count == 0)
            {
                resultText.text = "Przegrałeś!";
            }
            else
            {
                int totalWin = filteredPatterns.Sum(p => p.WinAmount);
                PlayerMoney += totalWin;
                resultText.text = $"Wygrałeś! Łączna wygrana: {totalWin:N0} zł.";

                if (filteredPatterns.Count == 1)
                {
                    var singleWin = filteredPatterns[0];
                    foreach (int index in singleWin.LineIndices)
                        slotTexts[index].color = singleWin.HighlightColor;
                }
                else
                {
                    yield return StartCoroutine(AnimateWinningPatterns(filteredPatterns));
                }
            }

            jackpot += jackpotIncrement;
            UpdateMoneyText();
            UpdateJackpotText();

            if (endSpinOnFinish)
            {
                isGameInProgress = false;
                EnableGameControls();
            }
        }

        private IEnumerator AnimateWinningPatterns(List<WinningPatternInfo> winningPatterns)
        {
            var sorted = winningPatterns.OrderBy(p => p.PatternData.Multiplier).ToList();

            foreach (var info in sorted)
            {
                foreach (var i in info.LineIndices)
                    slotTexts[i].color = info.HighlightColor;

                yield return new WaitForSeconds(highlightDelay);

                foreach (var i in info.LineIndices)
                    slotTexts[i].color = Color.gray;
            }

            foreach (var info in sorted)
            {
                foreach (var i in info.LineIndices)
                    slotTexts[i].color = Color.yellow;
            }
        }

        private Color GetColorForPattern(string patternName)
        {
            switch (patternName)
            {
                case "HORIZONTAL": return Color.red;
                case "VERTICAL": return Color.green;
                case "DIAGONAL": return Color.blue;
                case "HORIZONTAL-L": return new Color(1f, 0.5f, 0f);
                case "HORIZONTAL-XL": return new Color(1f, 0.2f, 0.2f);
                case "ZIG": case "ZAG": return Color.cyan;
                case "ABOVE": case "BELOW": return Color.magenta;
                case "EYE": return new Color(0.7f, 0.5f, 0.5f);
                case "JACKPOT": return Color.yellow;
                default: return Color.white;
            }
        }

        private void UpdateJackpotText()
        {
            jackpotText.text = $"Jackpot: {jackpot:N0} zł";
        }

        protected override void DisableGameControls()
        {
            base.DisableGameControls();
            autoSpinButton.interactable = false;
        }

        protected override void EnableGameControls()
        {
            base.EnableGameControls();
            autoSpinButton.interactable = true;
        }
    }
}
