namespace HayHustle
{
    /// <summary>A hired worker (paid a wage in coins every morning). Does one fixed job.</summary>
    public class Worker : Laborer
    {
        public WorkerKind Kind { get; private set; }
        public WorkerDef Def { get; private set; }

        public override string DisplayName => Def != null ? Def.Name : "Worker";
        protected override bool Ready => Def != null;

        public override JobKind Job => Kind switch
        {
            WorkerKind.Collector => JobKind.Collector,
            WorkerKind.WaterCarrier => JobKind.WaterCarrier,
            WorkerKind.FactoryHand => JobKind.FactoryHand,
            WorkerKind.Driver => JobKind.Driver,
            _ => JobKind.Guard,
        };

        public void Init(WorkerKind kind)
        {
            Kind = kind;
            Def = GameData.Workers[kind];
            SetupBody(Models.BuildWorker(kind, transform));
            GameManager.I.Workers.Add(this);
            RefreshTool();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (GameManager.I) GameManager.I.Workers.Remove(this);
        }
    }
}
