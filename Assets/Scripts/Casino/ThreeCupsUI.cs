using System.Collections;
using UnityEngine;

namespace Game.CasinoSystem
{
    public class ThreeCupsUI : Casino
    {
        [Header("Three Cups")]
        [SerializeField] private Transform cup;
        [SerializeField] private Transform cup1;
        [SerializeField] private Transform cup2;
        [SerializeField] private Transform ball;

        [Header("Animation")]
        [SerializeField] private float cupLiftHeight = 0.6f;
        [SerializeField] private float liftDuration = 0.25f;
        [SerializeField] private int shuffleCount = 12;
        [SerializeField] private float singleShuffleDuration = 0.075f;
        [SerializeField] private float revealDelayBeforeLift = 0.1f;

        [Header("Input")]
        [SerializeField] private Camera inputCamera;
        [SerializeField] private ThreeCupsSoundsManager sounds;

        private Vector3 ballOffset = new(0f, 0f, 0f);
        private readonly float avoidanceSideOffset = 0.35f;
        private readonly float shufflePauseBetweenMoves = 0.02f;
        private readonly float cupRadius = 0.25f;
        private readonly float edgeSwapCupRadius = 0.45f;
        private bool canChooseCup = false;
        private int ballCup = 0;
        private Transform[] cups;
        private Collider[] cupColliders;

        private float baseAdjacentDist;
        private float baseEdgeDist;

        protected override void Start()
        {
            base.Start();

            if (inputCamera == null) inputCamera = Camera.main;

            cups = new[] { cup, cup1, cup2 };
            cupColliders = new Collider[3];
            for (int i = 0; i < cups.Length; i++)
            {
                if (cups[i] == null)
                {
                    Debug.LogError($"ThreeCupsUI: Missing reference to cup {i + 1}.");
                    continue;
                }

                var col = cups[i].GetComponentInChildren<Collider>();
                if (col == null)
                {
                    Debug.LogError($"ThreeCupsUI: Missing Collider on cup {i + 1}.");
                }
                cupColliders[i] = col;
            }

            if (cup != null && cup1 != null && cup2 != null)
            {
                float d01 = HorizontalDistance(cup.position, cup1.position);
                float d12 = HorizontalDistance(cup1.position, cup2.position);
                baseAdjacentDist = (d01 + d12) * 0.5f;
                baseEdgeDist = HorizontalDistance(cup.position, cup2.position);

                if (baseAdjacentDist < 1e-4f || baseEdgeDist < baseAdjacentDist)
                {
                    baseAdjacentDist = Mathf.Max(baseAdjacentDist, 0.2f);
                    baseEdgeDist = Mathf.Max(baseEdgeDist, baseAdjacentDist * 1.5f);
                }
            }

            if (ball != null) ball.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!canChooseCup || isGameInProgress) return;
            if (Input.GetMouseButtonDown(0))
            {
                TryPickCupFromPointer();
            }
            else if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
            {
                TryPickCupFromPointer(Input.touches[0].position);
            }
        }

        public override void PlayGame()
        {
            if (betAmount > 0 && betAmount <= PlayerMoney)
            {
                resultText.text = "Where is the ball?";
                isGameInProgress = true;
                DisableGameControls();
                StartCoroutine(ShowBallAnimationCoroutine());
            }
            else
            {
                resultText.text = "Bet cannot be 0 PLN!";
            }
        }

        private IEnumerator ShowBallAnimationCoroutine()
        {
            ballCup = Random.Range(1, 4);
            var selectedCup = cups[ballCup - 1];

            yield return LiftCup(selectedCup, true);

            if (ball != null) ball.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.5f);

            yield return ShuffleCupsCoroutine();

            SetCupInteractivity(true);
            canChooseCup = true;
            resultText.text = "Choose a cup!";
            isGameInProgress = false;
        }

        private IEnumerator ShuffleCupsCoroutine()
        {
            SetCupInteractivity(false);

            for (int shuffle = 0; shuffle < shuffleCount; shuffle++)
            {
                int pattern = Random.Range(0, 4);

                switch (pattern)
                {
                    case 0:
                        yield return SwapCupsAvoidingCollision(cup, cup1, singleShuffleDuration);
                        break;
                    case 1:
                        yield return SwapCupsAvoidingCollision(cup1, cup2, singleShuffleDuration);
                        break;
                    case 2:
                        yield return SwapCupsAvoidingCollision(cup, cup2, singleShuffleDuration);
                        break;
                }

                sounds?.PlayShuffleWoosh();

                if (shufflePauseBetweenMoves > 0f)
                    yield return new WaitForSeconds(shufflePauseBetweenMoves);
            }
        }

        private IEnumerator SwapCupsAvoidingCollision(Transform a, Transform b, float duration)
        {
            Vector3 aStart = a.position;
            Vector3 bStart = b.position;

            Vector3 dir = (bStart - aStart);
            float len = dir.magnitude;
            if (len < 1e-4f)
                yield break;

            dir /= len;

            Vector3 perp = Vector3.Cross(Vector3.up, dir);
            if (perp.sqrMagnitude < 1e-6f) perp = Vector3.right;
            perp.Normalize();

            // Stały kierunek obejścia (brak losowości)
            const float sideSign = 1f;

            Vector3 mid = (aStart + bStart) * 0.5f;

            bool canUseDistance = baseAdjacentDist > 1e-4f && baseEdgeDist > baseAdjacentDist;
            bool isFarSwap;
            if (canUseDistance)
            {
                float currentDist = HorizontalDistance(aStart, bStart);
                float threshold = (baseAdjacentDist + baseEdgeDist) * 0.5f;
                isFarSwap = currentDist >= threshold;
            }
            else
            {
                isFarSwap = (a == cup && b == cup2) || (a == cup2 && b == cup);
            }

            float effectiveRadius = isFarSwap ? Mathf.Max(edgeSwapCupRadius, cupRadius) : cupRadius;
            float requiredD = Mathf.Max(avoidanceSideOffset, 2f * effectiveRadius);

            Vector3 aCtrl = mid + perp * (requiredD * sideSign);
            Vector3 bCtrl = mid - perp * (requiredD * sideSign);

            float aY = aStart.y;
            float bY = bStart.y;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;

                Vector3 aPos = BezierQuadratic(aStart, aCtrl, bStart, t);
                Vector3 bPos = BezierQuadratic(bStart, bCtrl, aStart, t);

                aPos.y = aY;
                bPos.y = bY;

                a.position = aPos;
                b.position = bPos;

                elapsed += Time.deltaTime;
                yield return null;
            }

            a.position = new Vector3(bStart.x, aY, bStart.z);
            b.position = new Vector3(aStart.x, bY, aStart.z);
        }

        private static float HorizontalDistance(Vector3 p, Vector3 q)
        {
            float dx = p.x - q.x;
            float dz = p.z - q.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static Vector3 BezierQuadratic(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            return uu * p0 + 2f * u * t * p1 + tt * p2;
        }

        public void ChooseCup(int cupNumber)
        {
            if (!canChooseCup)
            {
                resultText.text = "You need to set a bet and press Start!";
                return;
            }

            if (cupNumber < 1 || cupNumber > 3)
            {
                resultText.text = "Invalid cup number (1-3).";
                return;
            }

            canChooseCup = false;
            SetCupInteractivity(false);
            isGameInProgress = true;
            StartCoroutine(RevealAndResolveCoroutine(cupNumber));
        }

        private IEnumerator RevealAndResolveCoroutine(int cupNumber)
        {
            var chosenCup = cups[cupNumber - 1];

            bool hasBall = cupNumber == ballCup;
            if (hasBall && ball != null)
            {
                ball.position = chosenCup.position + ballOffset;
            }

            yield return LiftCup(chosenCup, hasBall);

            if (ball != null) ball.gameObject.SetActive(false);

            PlayThreeCups(betAmount, cupNumber);

            isGameInProgress = false;
            EnableGameControls();
        }

        private IEnumerator LiftCup(Transform cup, bool showBall)
        {
            float duration = liftDuration;
            float move = cupLiftHeight;

            Vector3 startPos = cup.position;
            Vector3 upPos = startPos + Vector3.up * move;

            bool shouldShowBall = showBall && ball != null;
            if (shouldShowBall)
            {
                ball.position = startPos + ballOffset;
                if (!ball.gameObject.activeSelf)
                    ball.gameObject.SetActive(true);

                if (revealDelayBeforeLift > 0f)
                    yield return new WaitForSeconds(revealDelayBeforeLift);
                else
                    yield return null;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                cup.position = Vector3.Lerp(startPos, upPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            cup.position = upPos;

            yield return new WaitForSeconds(0.3f);

            elapsed = 0f;
            while (elapsed < duration)
            {
                cup.position = Vector3.Lerp(upPos, startPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            cup.position = startPos;
        }

        private void PlayThreeCups(int bet, int chosenCup)
        {
            if (bet > PlayerMoney || bet <= 0 || chosenCup < 1 || chosenCup > 3)
            {
                resultText.text = "Invalid bet or cup number (1-3).";
                return;
            }

            PlayerMoney -= bet;

            if (chosenCup == ballCup)
            {
                int win = bet * 2;
                PlayerMoney += win;
                resultText.text = $"Bravo! The ball was under cup {ballCup}. Winnings: {win} PLN.";
            }
            else
            {
                resultText.text = $"Unfortunately, the ball was under cup {ballCup}. You lost!";
            }
            UpdateMoneyText();
        }

        private void SetCupInteractivity(bool interactable)
        {
            for (int i = 0; i < cupColliders.Length; i++)
            {
                if (cupColliders[i] != null)
                    cupColliders[i].enabled = interactable;
            }
        }

        protected override void DisableGameControls()
        {
            base.DisableGameControls();
            SetCupInteractivity(false);
        }

        protected override void EnableGameControls()
        {
            base.EnableGameControls();
        }

        //only for testing in editor   
        private void TryPickCupFromPointer(Vector2? screenPosOverride = null)
        {
            Vector2 screenPos = screenPosOverride ?? (Vector2)Input.mousePosition;
            if (inputCamera == null) return;

            Ray ray = inputCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                for (int i = 0; i < cups.Length; i++)
                {
                    if (cups[i] == null) continue;

                    if (hit.collider.transform == cups[i] || hit.collider.transform.IsChildOf(cups[i]))
                    {
                        ChooseCup(i + 1);
                        return;
                    }
                }
            }
        }
    }
}
