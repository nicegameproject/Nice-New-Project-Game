using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text; // upewnij się, że jest dodane na górze pliku

namespace Game.CasinoSystem
{
    public class SlotMachineUI : Casino
    {
        [Header("Slot Machine Specific")]
        [SerializeField] private TextMeshProUGUI jackpotText;
        [SerializeField] private float spinDuration = 1.5f;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private int jackpotIncrement = 10;
        [SerializeField] private float highlightDelay = 1.0f;

        private int jackpot = 1000;
        private readonly string[] symbols = { "‡", "†", "‰", "♣", "♦", "♥", "♠" };
        private readonly int[] symbolMultipliers = { 1, 2, 3, 4, 5, 6, 7 };
        private readonly float[] chanceToShow = { 19.4f, 19.4f, 14.9f, 14.9f, 11.9f, 11.9f, 7.6f };

        [SerializeField] private Reel[] reels = new Reel[5];

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
            resultText.text = "";

            int visibleSymbols = 3;
            int totalSymbols = reels.Length * visibleSymbols;

            string[] board = new string[totalSymbols];
            for (int i = 0; i < totalSymbols; i++)
                board[i] = symbols[GetRandomSymbolIndexBasedOnChance()];

            List<Coroutine> spinning = new();
            for (int c = 0; c < reels.Length; c++)
            {
                string[] forcedSymbols = new string[visibleSymbols];
                for (int r = 0; r < visibleSymbols; r++)
                    forcedSymbols[r] = board[r * reels.Length + c];

                spinning.Add(StartCoroutine(reels[c].Spin(spinDuration + c * 0.8f, symbols, forcedSymbols)));
            }
            foreach (var spin in spinning) yield return spin;

            PlayerMoney -= bet;

            var allFoundPatterns = new List<WinningPatternInfo>();
            foreach (var pattern in this.patterns)
            {
                foreach (var line in pattern.Lines)
                {
                    string firstSymbol = board[line[0]];
                    if (line.All(index => board[index] == firstSymbol))
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

            DebugLogSpin(board, filteredPatterns, bet);

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
                    {
                        int reelIdx = index % 5;
                        int rowIdx = index / 5;
                        reels[reelIdx].HighlightSymbol(rowIdx, singleWin.HighlightColor);
                    }
                }
                else
                {
                    yield return StartCoroutine(AnimateWinningPatternsReel(filteredPatterns, reels));
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

        private IEnumerator AnimateWinningPatternsReel(List<WinningPatternInfo> winningPatterns, Reel[] reels)
        {
            var sorted = winningPatterns.OrderBy(p => p.PatternData.Multiplier).ToList();

            foreach (var info in sorted)
            {
                foreach (var index in info.LineIndices)
                {
                    int reelIdx = index % 5;
                    int rowIdx = index / 5;
                    reels[reelIdx].HighlightSymbol(rowIdx, info.HighlightColor);
                }

                yield return new WaitForSeconds(highlightDelay);

                foreach (var index in info.LineIndices)
                {
                    int reelIdx = index % 5;
                    int rowIdx = index / 5;
                    reels[reelIdx].HighlightSymbol(rowIdx, Color.gray);
                }
            }

            foreach (var info in sorted)
            {
                foreach (var index in info.LineIndices)
                {
                    int reelIdx = index % 5;
                    int rowIdx = index / 5;
                    reels[reelIdx].HighlightSymbol(rowIdx, Color.yellow);
                }
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

        private void DebugLogSpin(string[] board, List<WinningPatternInfo> winningPatterns, int bet)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Slot] Final board (visual rows):");

            int[] rowMap = { 2, 1, 0 };

            for (int vr = 0; vr < 3; vr++)
            {
                int r = rowMap[vr];
                var row = new string[5];
                for (int c = 0; c < 5; c++)
                    row[c] = board[r * 5 + c];

                sb.AppendLine($"Row {vr}: [{string.Join(" | ", row)}]");
            }

            if (winningPatterns == null || winningPatterns.Count == 0)
            {
                sb.AppendLine("Result: LOSS (no winning patterns)");
                Debug.Log(sb.ToString());
                return;
            }

            int totalWin = winningPatterns.Sum(p => p.WinAmount);
            sb.AppendLine($"Result: WIN, total = {totalWin} (bet={bet})");
            foreach (var wp in winningPatterns.OrderByDescending(p => p.FinalMultiplier))
            {
                string symbol = board[wp.LineIndices[0]];
                sb.AppendLine(
                    $"- {wp.PatternData.Name} x{wp.PatternData.Multiplier:0.##} | symbol '{symbol}' | final x{wp.FinalMultiplier:0.##} | win {wp.WinAmount} | idx [{string.Join(", ", wp.LineIndices)}]"
                );
            }

            Debug.Log(sb.ToString());
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
