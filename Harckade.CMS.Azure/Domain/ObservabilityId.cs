namespace Harckade.CMS.Azure.Domain
{
    public class ObservabilityId
    {
        public Guid Value { get; private set; }
        public ObservabilityId()
        {
            Value = Guid.NewGuid();
        }

        public override string ToString()
        {
            return $"{Value}";
        }
    }
}
