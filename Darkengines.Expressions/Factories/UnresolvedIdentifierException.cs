using System;
using System.Runtime.Serialization;

namespace Darkengines.Expressions.Factories {
	[Serializable]
	internal class UnresolvedIdentifierException : Exception {
		public UnresolvedIdentifierException() {
		}

		public UnresolvedIdentifierException(string message) : base(message) {
		}

		public UnresolvedIdentifierException(string message, Exception innerException) : base(message, innerException) {
		}

		protected UnresolvedIdentifierException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}