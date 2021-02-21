
/*

Other things to consider:

- Plural name: for resources, infer by default
- Display name: for fields and resources, infer by default
- Description: for fields and resources
- Unit: for numeric fields
- Data kind: for fields, helps with sample generation
- Encryption: for fields
- Unicity: for fields
- Trimming: for string fields
- Mirroring: for relational fields
- Extra validation: for fields and resources, custom validation = extra validation + no other validation...
- Conditionality: for fields, i.e. bird { can fly : bool, max flight altitude : float -- if(can fly) }
- Participates in master view: for fields
- Immutability: for fields

~ I18N
~ Auth: by resource type / resource instance / operation / user

- Usage frequency: for operations, helps with caching
~ Rate limiter

~ Filter/Search: for fields
~ Paginate
~ Order
~ Hateoas
~ Soft delete: for resources
~ Updates as events, helps with history

*/

/*

option => primitive C# enum
flags => C# flags enum
case => Kotlin sealed class

*/

class Function {
	public Function(string equation) {}
	public Function(Func<float, float> @delegate) {} // Is it even possible? Is it even desirable?
	public Function(Expression<Func<float, float>> expression) {}

	readonly object[] Instructions;
	Function(object[] instructions) => Instructions = instructions;

	public Expression<Func<float, float>> Expression { get; }

	public override string ToString() => "y = f(x)";

	// Df: How to compute? How to infer? How to validate instructions against it? How to validate it against instructions?
	// Domain => x v.s. Range => y @see https://www.onlinemathlearning.com/image-files/domain-range.png
	// Validation: How?
	// Json Serialization: Stack-based: [
	//    "x", "x", "*",
	//    2, "x", "*",
	//    3,
	//    "+",
	//    "+"
	// ]
}
