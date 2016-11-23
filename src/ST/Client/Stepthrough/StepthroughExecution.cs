using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StoryTeller;
using StoryTeller.Engine;
using StoryTeller.Engine.UserInterface;
using StoryTeller.Messages;

namespace ST.Client.Stepthrough
{
    public class StepthroughExecution : SpecExecution
    {
        private readonly IUserInterfaceObserver _observer;
        private int _position = -1;
        private IList<ILineExecution> _steps;

        private readonly TaskCompletionSource<bool> _completion = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<bool> _hasStarted = new TaskCompletionSource<bool>();

        public StepthroughExecution(SpecExecutionRequest request, StopConditions stopConditions, IUserInterfaceObserver observer) : base(request, stopConditions, new InstrumentedLogger(observer))
        {
            _observer = observer;
        }

        protected override Task setupTimeout()
        {
            // Need this to do nothing
            return new Task(() => {});
        }

        public SpecContext Context { get; private set; }

        public Task HasStarted => _hasStarted.Task;

        protected override Task executeSteps(SpecContext context, IList<ILineExecution> lines, CancellationToken token)
        {
            _steps = lines;
            Context = context;

            _observer.SendProgress(new SpecProgress(Context.Specification.id, Context.Counts, 0, _steps.Count));

            if (Request.Mode == ExecutionMode.stepthrough)
            {
                sendNextStepMessage();
            }
            else if (Request.Mode == ExecutionMode.breakpoint)
            {
                RunToBreakpoint();
            }
            else
            {
                return base.executeSteps(context, lines, token);
            }

            _hasStarted.SetResult(true);

            return _completion.Task;
        }

        public override void Cancel()
        {
            base.Cancel();

            _completion.TrySetResult(true);
        }

        private void moveNext()
        {
            _position++;
        }

        public Task Completed => _completion.Task;

        public ILineExecution Current => _position == -1 || _position == _steps.Count ? null : _steps[_position];

        public ILineExecution Next => (_position + 1) >= _steps.Count ? null : _steps[_position + 1];

        public void RunNext()
        {
            moveNext();
            if (Current == null)
            {
                finish();
            }
            else
            {
                executeCurrentStep();
                sendNextStepMessage();
            }
        }

        private void finish()
        {
            _completion.SetResult(true);
        }

        private void executeCurrentStep()
        {
            Current.Execute(Context);
            _observer.SendProgress(new SpecProgress(Request.Id, Context.Counts, _position + 1, _steps.Count));
        }

        public void RunToEnd()
        {
            while (Next != null)
            {
                moveNext();
                executeCurrentStep();
            }

            finish();
        }

        public void RunToBreakpoint()
        {
            if (isAtBreakpoint())
            {
                moveNext();
                executeCurrentStep();

                if (!isAtBreakpoint())
                {
                    RunToBreakpoint();
                }
                else
                {
                    sendNextOrFinishedMessage();
                }



                return;
            }

            while (Next != null && !isAtBreakpoint())
            {
                moveNext();
                executeCurrentStep();
            }

            sendNextOrFinishedMessage();
        }

        private void sendNextOrFinishedMessage()
        {
            if (Next == null)
            {
                finish();
            }
            else
            {
                sendNextStepMessage();
            }
        }

        private bool isAtBreakpoint()
        {
            var matchesBreakpoint = Request.Specification.MatchesBreakpoint(Next.Id, Next.Position);
            return matchesBreakpoint;
        }

        private void sendNextStepMessage()
        {
            _observer.SendToClient(new NextStep(Request.Id, Next));
        }
    }
}