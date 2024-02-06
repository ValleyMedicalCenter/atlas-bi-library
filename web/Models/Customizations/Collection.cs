using System.ComponentModel.DataAnnotations.Schema;
using Atlas_Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Atlas_Web.Models
{
    public interface Collection__Metadata { }

    [ModelMetadataType(typeof(Collection__Metadata))]
    public partial class Collection
    {
        [NotMapped]
        public virtual string LastUpdatedDateDisplayString
        // don't display the time portion if > 24 hrs ago
        {
            get { return ModelHelpers.RelativeDate(LastUpdateDate); }
        }
    }
}
