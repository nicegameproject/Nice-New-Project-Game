using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.CasinoSystem
{
    public class SlotMachineUI : Casino
    {
        [Header("Slot Machine Specific")]
        [SerializeField] private TextMeshProUGUI jackpotText;
        [SerializeField] private float spinDuration = 2f;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private int jackpotIncrement = 10;
        [SerializeField] private int autoSpinAmount = 10;
        [SerializeField] private float timeAfterSpinAgain = 0.5f;
        [SerializeField] private float normalHighlightDelay = 0.5f;
        [SerializeField] private Reel[] reels = new Reel[5];
        [SerializeField] private Transform leverHandle;

        [Header("Developer Tools")]
        [SerializeField] private bool developerCheat = false;

        [Header("Equipped Item")]
        [SerializeField] private SlotItemSO equippedItem;

        [Header("Audio")]
        [SerializeField] private SlotMachineSoundsManager soundsManager;

        private int jackpot = 1000;
        private readonly string[] symbols = { "‡", "†", "‰", "♣", "♦", "♥", "♠" };
        private readonly int[] symbolMultipliers = { 1, 2, 3, 4, 5, 6, 7 };
        private readonly float[] chanceToShow = { 19.4f, 19.4f, 14.9f, 14.9f, 11.9f, 11.9f, 7.6f };

        // Runtime copies modified by the equipped item on each spin.
        private int[] runtimeSymbolMultipliers;
        private float[] runtimeChanceToShow;

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

            RebuildRuntimeSymbolMultipliers(consumeUse: false);
            UpdateJackpotText();

            if (symbols.Length != runtimeChanceToShow.Length || symbols.Length != runtimeSymbolMultipliers.Length)
            {
                Debug.LogError("The number of symbols, their multipliers and chances of appearing must be the same!");
            }
        }

        private void RebuildRuntimeSymbolMultipliers(bool consumeUse)
        {
            runtimeSymbolMultipliers = symbolMultipliers.ToArray();
            runtimeChanceToShow = chanceToShow.ToArray();

            if (equippedItem != null)
            {
                bool consumeForThisStep = consumeUse && !(equippedItem is ForceHighPatternItemSO);
                equippedItem.TryApply(runtimeSymbolMultipliers, runtimeChanceToShow, consumeForThisStep);
            }
        }

        public override void PlayGame()
        {
            StartCoroutine(PlayGameRoutine());
        }

        private IEnumerator PlayGameRoutine()
        {
            if (betAmount > PlayerMoney)
            {
                resultText.text = "You don't have enough money!";
                yield break;
            }

            isGameInProgress = true;
            DisableGameControls();

            yield return StartCoroutine(AnimateLever());

            yield return StartCoroutine(SpinSlots(betAmount, true));
        }

        private IEnumerator AnimateLever()
        {
            if (leverHandle == null) yield break;

            if (soundsManager != null)
                soundsManager.PlayLeverPull();

            float downDuration = 0.15f;
            float upDuration = 0.2f;
            float downAngle = -100f;

            Sequence seq = DOTween.Sequence()
                .Append(leverHandle.DORotate(new Vector3(downAngle, 0, 0), downDuration).SetEase(Ease.InQuad))
                .Append(leverHandle.DORotate(new Vector3(-30, 0, 0), upDuration).SetEase(Ease.OutQuad));

            yield return seq.WaitForCompletion();
        }

        public void OnAutoSpinClicked()
        {
            isGameInProgress = true;
            DisableGameControls();
            StartCoroutine(DeveloperCheatAutoSpin(autoSpinAmount));
        }

        private IEnumerator DeveloperCheatAutoSpin(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (betAmount > PlayerMoney)
                {
                    resultText.text = "You don't have enough money for more spins!!";
                    break;
                }

                // (Opcjonalnie) animacja dźwigni przed każdym automatycznym spinem
                yield return StartCoroutine(AnimateLever());

                yield return StartCoroutine(SpinSlots(betAmount, false));
                yield return new WaitForSeconds(timeAfterSpinAgain);
            }

            isGameInProgress = false;
            EnableGameControls();
        }

        private IEnumerator SpinSlots(int bet, bool endSpinOnFinish)
        {
            resultText.text = "XXXX";

            RebuildRuntimeSymbolMultipliers(consumeUse: true);

            int visibleSymbols = 3;
            int totalSymbols = reels.Length * visibleSymbols;

            string[] board = new string[totalSymbols];
            for (int i = 0; i < totalSymbols; i++)
                board[i] = symbols[GetRandomSymbolIndexBasedOnChance()];

            if (developerCheat)
            {
                ForceHighMultiplierPattern(board);
            }

            if (equippedItem is ForceHighPatternItemSO forceItem && forceItem.CanUse)
            {
                ForceHighMultiplierPattern(board);
                forceItem.TryApply(null, null, consumeUse: true);
            }

            if (soundsManager != null) soundsManager.StartReelsLoop();

            var spinning = new List<Coroutine>();
            var reelOrder = new List<int>();
            for (int c = 0; c < reels.Length; c++)
            {
                string[] forcedSymbols = new string[visibleSymbols];
                for (int r = 0; r < visibleSymbols; r++)
                    forcedSymbols[r] = board[r * reels.Length + c];

                spinning.Add(StartCoroutine(reels[c].Spin(spinDuration + c * 0.5f, symbols, forcedSymbols)));
                reelOrder.Add(c);
            }

            for (int i = 0; i < spinning.Count; i++)
            {
                yield return spinning[i];
                if (soundsManager != null)
                {
                    var t = reels[reelOrder[i]].transform;
                    soundsManager.PlayReelStopAt(t.position);
                }
            }

            if (soundsManager != null) soundsManager.StopReelsLoop();

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
                            int symbolMultiplier = runtimeSymbolMultipliers[symbolIndex];
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

            List<WinningPatternInfo> filteredPatterns;
            bool hasJackpot = allFoundPatterns.Any(p => p.PatternData.Name == "JACKPOT");

            if (hasJackpot)
            {
                var allowedVertical = new List<int[]>
                {
                    new[] {0, 5, 10},
                    new[] {2, 7, 12},
                    new[] {4, 9, 14}
                };
                var allowedDiagonal = new List<int[]>
                {
                    new[] {1, 7, 13},
                    new[] {11, 7, 3}
                };

                bool IsAllowedVertical(int[] line) => allowedVertical.Any(av => av.SequenceEqual(line));
                bool IsAllowedDiagonal(int[] line) => allowedDiagonal.Any(ad => ad.SequenceEqual(line));

                filteredPatterns = allFoundPatterns.Where(p =>
                    p.PatternData.Name == "JACKPOT" ||
                    p.PatternData.Name == "EYE" ||
                    p.PatternData.Name == "ABOVE" ||
                    p.PatternData.Name == "BELOW" ||
                    p.PatternData.Name == "HORIZONTAL-XL" ||
                    (p.PatternData.Name == "VERTICAL" && IsAllowedVertical(p.LineIndices)) ||
                    (p.PatternData.Name == "DIAGONAL" && IsAllowedDiagonal(p.LineIndices))
                ).ToList();
            }
            else
            {
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

                filteredPatterns = allFoundPatterns.Where(p => !patternsToRemove.Contains(p)).ToList();
            }

            DebugLogSpin(board, filteredPatterns, bet);

            if (filteredPatterns.Count == 0)
            {
                resultText.text = "You lost!";
                soundsManager.PlayLostClip();
            }
            else
            {
                int totalWin = filteredPatterns.Sum(p => p.WinAmount);
                PlayerMoney += totalWin;
                resultText.text = $"You won! {totalWin:N0} zł.";

                if (filteredPatterns.Count == 1)
                {
                    var singleWin = filteredPatterns[0];

                    if (soundsManager != null)
                    {
                        var oneLine = new List<int[]>(1) { singleWin.LineIndices };
                        soundsManager.StartWinSequence(oneLine, normalHighlightDelay);
                    }

                    foreach (int index in singleWin.LineIndices)
                    {
                        int reelIdx = index % 5;
                        int rowIdx = index / 5;
                        reels[reelIdx].HighlightSymbol(rowIdx, singleWin.HighlightColor);
                        reels[reelIdx].PulseSymbol(rowIdx);
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

        private int GetRandomSymbolIndexBasedOnChance()
        {
            float totalChance = runtimeChanceToShow.Sum();
            float randomValue = Random.Range(0, totalChance);
            float cumulativeChance = 0f;

            for (int i = 0; i < runtimeChanceToShow.Length; i++)
            {
                cumulativeChance += runtimeChanceToShow[i];
                if (randomValue < cumulativeChance)
                {
                    return i;
                }
            }
            return runtimeChanceToShow.Length - 1;
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

        private void ForceHighMultiplierPattern(string[] board)
        {
            var eligible = patterns.Where(p => p.Multiplier >= 7.0f).ToList();
            if (eligible.Count == 0) return;

            var chosenPattern = eligible[Random.Range(0, eligible.Count)];
            var chosenLine = chosenPattern.Lines[Random.Range(0, chosenPattern.Lines.Length)];
            string chosenSymbol = symbols[GetRandomSymbolIndexBasedOnChance()];

            foreach (var idx in chosenLine)
            {
                board[idx] = chosenSymbol;
            }
        }

        private IEnumerator AnimateWinningPatternsReel(List<WinningPatternInfo> winningPatterns, Reel[] reels)
        {
            var sorted = winningPatterns.OrderBy(p => p.PatternData.Multiplier).ToList();

            Coroutine audioRoutine = null;
            if (soundsManager != null)
            {
                var lines = new List<int[]>(sorted.Count);
                for (int i = 0; i < sorted.Count; i++)
                    lines.Add(sorted[i].LineIndices);

                audioRoutine = soundsManager.StartWinSequence(lines, normalHighlightDelay);
            }

            int step = 0;
            foreach (var info in sorted)
            {
                foreach (var index in info.LineIndices)
                {
                    int reelIdx = index % 5;
                    int rowIdx = index / 5;
                    reels[reelIdx].HighlightSymbol(rowIdx, info.HighlightColor);
                    reels[reelIdx].PulseSymbol(rowIdx);
                }

                yield return new WaitForSeconds(normalHighlightDelay);

                foreach (var index in info.LineIndices)
                {
                    int reelIdx = index % 5;
                    int rowIdx = index / 5;
                    reels[reelIdx].StopPulse(rowIdx);
                    reels[reelIdx].HighlightSymbol(rowIdx, Color.gray);
                }

                step++;
            }

            if (audioRoutine != null)
                yield return audioRoutine;

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
                case "ZIG":
                case "ZAG": return Color.cyan;
                case "ABOVE":
                case "BELOW": return Color.magenta;
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
            sb.AppendLine("[Slot] Final board");

            if (winningPatterns == null || winningPatterns.Count == 0)
            {
                sb.AppendLine("Result: LOSS (no winning patterns)");
                Debug.Log(sb.ToString());
                return;
            }

            foreach (var wp in winningPatterns.OrderByDescending(p => p.FinalMultiplier))
            {
                string symbol = board[wp.LineIndices[0]];
                sb.AppendLine(
                    $"- {wp.PatternData.Name} x{wp.PatternData.Multiplier:0.##} | Symbol '{symbol}' | Final Multiplier x{wp.FinalMultiplier:0.##} | Win {wp.WinAmount}"
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
