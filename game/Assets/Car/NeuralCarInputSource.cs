using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Car
{
    [RequireComponent(typeof(CarVision), typeof(SpawnOnStartLine), typeof(CarController))]
    public class NeuralCarInputSource : MonoBehaviour, ICarInputSource
    {
        private static readonly int[] CHECKPOINT_VISION_OFFSETS = new[] { 5, 10, 15, 20, 25 };

        public bool IsActive { get; private set; }

        public double ThrottleBreak => _outputs[0];

        public double Steering => _outputs[1];

        private double[] _inputs;

        private double[] _outputs;

        private NeuralNetworkTrainer _trainer;

        private CheckpointGenerator _checkpointGenerator;

        private LongRunningTimer _timer;

        private ulong _memberIndex;

        private CarVision _visionSource;

        private Rigidbody2D _rigidbody2D;

        private SpriteRenderer _renderer;

        private CarController _carController;

        private int _maxCheckpoint = 0;

        private double _maxCheckpointReachedAt;

        private Color _color;

        [SerializeField]
        private double _checkpointTimeoutSeconds = 10f;

        public int Checkpoint { get; private set; } = 0;

        public double? LapFinishTime { get; private set; } = null;

        private void Awake()
        {
            _visionSource = GetComponent<CarVision>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _carController = GetComponent<CarController>();

            _color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.75f, 1f);
        }

        public void Initialize(NeuralNetworkTrainer trainer, ulong memberIndex, CheckpointGenerator checkpointGenerator, LongRunningTimer timer)
        {
            _trainer = trainer;
            _memberIndex = memberIndex;
            _inputs = new double[trainer.Population.Inputs];
            _outputs = new double[trainer.Population.Outputs];

            _checkpointGenerator = checkpointGenerator;
            _timer = timer;

            IsActive = false;
        }

        public void ActivateAndStartDriving()
        {
            IsActive = true;

            _maxCheckpointReachedAt = _timer.Now;
        }

        public void DeactivateAndStall()
        {
            IsActive = false;

            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
        }

        public void ResetRun()
        {
            Checkpoint = 0;
            LapFinishTime = null;

            _maxCheckpoint = 0;

            DeactivateAndStall();

            _visionSource.ResetAge();

            for (int i = 2; i < _outputs.Length; i++)
            {
                // Reset feedback outputs/inputs
                _outputs[i] = 0.0;
            }

            GetComponent<SpawnOnStartLine>().Respawn();
        }

        public void AdvanceTimestep()
        {
            if (!IsActive)
            {
                return;
            }

            if (_timer.Now - _maxCheckpointReachedAt > _checkpointTimeoutSeconds * _trainer.Leniency)
            {
                IsActive = false;
                return;
            }

            // Assemble inputs
            try
            {
                var idx = 0;

                // Vision (5)
                _visionSource.Recalculate();

                _inputs[idx++] = _visionSource.Left;
                _inputs[idx++] = _visionSource.FrontLeft;
                _inputs[idx++] = _visionSource.Front;
                _inputs[idx++] = _visionSource.FrontRight;
                _inputs[idx++] = _visionSource.Right;

                // Signed velocity (1)
                _inputs[idx++] = Vector3.Dot(_rigidbody2D.velocity, transform.up);

                // Signed distance to center line when driving straight (1)
                _inputs[idx++] = _visionSource.Right - _visionSource.Left;

                // Square velocity (1)
                _inputs[idx++] = ScaleInputWithGeneration(_rigidbody2D.velocity.sqrMagnitude, 60, 90);

                // Upcoming checkpoint distances CHECKPOINT_VISION_OFFSETS.Length * 2
                foreach (var offset in CHECKPOINT_VISION_OFFSETS)
                {
                    var pos = GetCheckpointPosLocal(offset);
                    _inputs[idx++] = ScaleInputWithGeneration(pos.x, 90, 150);
                    _inputs[idx++] = ScaleInputWithGeneration(pos.y, 90, 150);
                }

                // 2D Velocity (2)
                _inputs[idx++] = ScaleInputWithGeneration(_rigidbody2D.velocity.x, 150, 180);
                _inputs[idx++] = ScaleInputWithGeneration(_rigidbody2D.velocity.y, 150, 180);

                // For every output with an index larger than two (speed and steering are the first two),
                // feed it back into the network as an input
                for (int i = 0; i < _outputs.Length - 2; i++)
                {
                    _inputs[idx++] = ScaleInputWithGeneration(_outputs[i + 2], 180, 240);
                }

            }
            catch { }

            _trainer.Population.EvaluateMember(_memberIndex, _inputs, _outputs);

            _carController.AddForces();
        }

        /// <summary>
        /// Scales the input so that is is 0 in generation <see cref="minGeneration"/> and before, and 1 in generation <see cref="maxGeneration"/>
        /// </summary>
        private double ScaleInputWithGeneration(double value, int minGeneration, int maxGeneration)
        {
            return value * Mathf.Clamp01((float)(_trainer.Generation - minGeneration) / (float)(maxGeneration - minGeneration));
        }

        /// <summary>
        /// Returns an upcoming checkpoint's position (based on the current checkpoint) in local space
        /// </summary>
        /// <param name="checkpointOffset"></param>
        /// <returns></returns>
        private Vector2 GetCheckpointPosLocal(int checkpointOffset)
        {
            var cp = _checkpointGenerator.GetCheckpointPos(Checkpoint + checkpointOffset);
            return transform.InverseTransformPoint(cp);
        }

        private void Update()
        {
            // TODO: Move this somewhere smarter
            if (IsActive)
            {
                _renderer.color = _color;
            }
            else
            {
                _renderer.color = _color * 0.5f;
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            DeactivateAndStall();
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!IsActive)
            {
                return;
            }

            if (int.TryParse(col.gameObject.name, out var cp))
            {
                if (cp > Checkpoint + CheckpointGenerator.NUM_CHECKPOINTS / 2)
                {
                    //Debug.LogWarning($"Car {_memberIndex} violated the CP skip limit (Current: {Checkpoint}, Ignored: {cp})");
                    return;
                }

                Checkpoint = cp;

                if (cp > _maxCheckpoint)
                {
                    _maxCheckpoint = cp;
                    _maxCheckpointReachedAt = _timer.Now;
                }

                if (cp == CheckpointGenerator.NUM_CHECKPOINTS)
                {
                    LapFinishTime = _timer.Now;
                    DeactivateAndStall();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Gizmos.color = Color.green;

            foreach (var offset in CHECKPOINT_VISION_OFFSETS)
            {

                var pos = GetCheckpointPosLocal(offset);                

                Gizmos.DrawRay(transform.position, transform.TransformPoint(pos) - transform.position);
            }
        }
    }
}
