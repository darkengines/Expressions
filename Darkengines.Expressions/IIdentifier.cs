using System.Linq.Expressions;

namespace Darkengines.Expressions {
	public interface IIdentifier {
		string Name { get; }
		Expression Expression { get; }
	}
}