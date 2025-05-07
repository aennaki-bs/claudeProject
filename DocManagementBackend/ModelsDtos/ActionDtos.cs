namespace DocManagementBackend.Models
{
    public class CreateActionDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ActionDto
    {
        public int ActionId { get; set; }
        public string ActionKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AssignActionToStepDto
    {
        public int StepId { get; set; }
        public int ActionId { get; set; }
        public List<StatusEffectDto>? StatusEffects { get; set; }
    }

    public class StatusEffectDto
    {
        public int StatusId { get; set; }
        public bool SetsComplete { get; set; }
    }
}