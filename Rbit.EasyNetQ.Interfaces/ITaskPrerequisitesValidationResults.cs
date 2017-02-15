using System.Collections.Generic;

namespace Rbit.EasyNetQ.Interfaces
{
    public interface ITaskPrerequisitesValidationResults
    {
        PrerequisitesValidationResult Result { get; set; }

        IEnumerable<string> ValidationMessages { get; set; }
    }
}

