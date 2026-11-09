using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PortalTarefas.Web.Observability;

public static class PortalTelemetry
{
    public const string ActivitySourceName = "PortalTarefas";
    public const string MeterName = "PortalTarefas";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> TarefasCriadas =
        Meter.CreateCounter<long>("portaltarefas.tarefas.criadas");

    public static readonly Histogram<double> DuracaoCadastro =
        Meter.CreateHistogram<double>(
            "portaltarefas.cadastro.duracao",
            unit: "ms");
}
