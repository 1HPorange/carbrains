using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Car
{
    [RequireComponent(typeof(CarVision), typeof(SpawnOnStartLine))]
    public class NeuralCarInputSource : MonoBehaviour, ICarInputSource
    {
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

            GetComponent<SpawnOnStartLine>().Respawn();
        }

        private void FixedUpdate()
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

                // Signed distance to center line when driving straight (1)
                _inputs[idx++] = _visionSource.Right - _visionSource.Left;

                // Signed velocity (1)
                _inputs[idx++] = Vector3.Dot(_rigidbody2D.velocity, transform.up);

                // Square velocity (1)
                _inputs[idx++] = _rigidbody2D.velocity.sqrMagnitude;

                // 2D Velocity (2)
                _inputs[idx++] = _rigidbody2D.velocity.x;
                _inputs[idx++] = _rigidbody2D.velocity.y;

                // Upcoming checkpoint distances (10, 20, 30, 40) (8)
                var cpDistance = _checkpointGenerator.GetCheckpointPos(Checkpoint + 10) - (Vector2)transform.position;
                _inputs[idx++] = cpDistance.x;
                _inputs[idx++] = cpDistance.y;

                cpDistance = _checkpointGenerator.GetCheckpointPos(Checkpoint + 20) - (Vector2)transform.position;
                _inputs[idx++] = cpDistance.x;
                _inputs[idx++] = cpDistance.y;

                cpDistance = _checkpointGenerator.GetCheckpointPos(Checkpoint + 30) - (Vector2)transform.position;
                _inputs[idx++] = cpDistance.x;
                _inputs[idx++] = cpDistance.y;

                cpDistance = _checkpointGenerator.GetCheckpointPos(Checkpoint + 40) - (Vector2)transform.position;
                _inputs[idx++] = cpDistance.x;
                _inputs[idx++] = cpDistance.y;
            }
            catch { }


            _trainer.Population.EvaluateMember(_memberIndex, _inputs, _outputs);
        }

        private void Update()
        {
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
    }
}
