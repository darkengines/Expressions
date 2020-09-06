using Microsoft.EntityFrameworkCore.Metadata;

namespace Darkengines.Expressions {
	public interface IModelProvider {
		IModel Model { get; }
	}
}
