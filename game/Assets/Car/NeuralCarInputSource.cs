using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

        private Population _population;

        private ulong _memberIndex;

        private CarVision _visionSource;

        private Rigidbody2D _rigidbody2D;

        private int _maxCheckpoint = 0;

        private DateTime _maxCheckpointReachedAt;

        [SerializeField]
        private double _checkpointTimeoutSeconds = 10f;

        public int Checkpoint { get; private set; } = 0;

        public DateTime? LapFinishTime { get; private set; } = null;

        private void Awake()
        {
            _visionSource = GetComponent<CarVision>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        public void Initialize(Population population, ulong memberIndex)
        {
            _population = population;
            _memberIndex = memberIndex;
            _inputs = new double[population.Inputs];
            _outputs = new double[population.Outputs];

            IsActive = false;
        }

        public void ActivateAndStartDriving()
        {
            IsActive = true;

            _maxCheckpointReachedAt = DateTime.Now;
        }

        public void DeactivateAndStall()
        {
            IsActive = false;

            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
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

            if ((DateTime.Now - _maxCheckpointReachedAt).TotalSeconds > _checkpointTimeoutSeconds)
            {
                IsActive = false;
                return;
            }

            _inputs[0] = Vector2.Angle(_rigidbody2D.velocity, transform.up) > 90f ? 
                -_rigidbody2D.velocity.magnitude : 
                _rigidbody2D.velocity.magnitude;
            _inputs[1] = _visionSource.Left;
            _inputs[2] = _visionSource.FrontLeft;
            _inputs[3] = _visionSource.Front;
            _inputs[4] = _visionSource.FrontRight;
            _inputs[5] = _visionSource.Right;

            _population.EvaluateMember(_memberIndex, _inputs, _outputs);
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
                    Debug.LogWarning($"Car {_memberIndex} violated the CP skip limit (Current: {Checkpoint}, Ignored: {cp})");
                    return;
                }

                Checkpoint = cp;

                if (cp > _maxCheckpoint)
                {
                    _maxCheckpoint = cp;
                    _maxCheckpointReachedAt = DateTime.Now;
                }

                if (cp == CheckpointGenerator.NUM_CHECKPOINTS)
                {
                    LapFinishTime = DateTime.Now;
                    DeactivateAndStall();
                }
            }
        }
    }
}
