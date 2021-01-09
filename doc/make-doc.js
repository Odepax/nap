const fs = require("fs")
const xmldoc = require("xmldoc") // Browser alternative: https://developer.mozilla.org/en-US/docs/Web/API/DOMParser

Array.prototype.toMap = function toMap(keySelector) {
	return new Map(this.map(it => [ keySelector(it), it ]))
}

class Type {
	/**
	 * @param {string} name
	 * @param {string} doc
	 * @param {Generic[]} generics
	 * @param {Constraint[]} constraints
	 */
	constructor(name, doc, generics = [], constraints = []) {
		this.name = name
		this.doc = doc
		this.generics = generics
		this.constraints = constraints
	}
}

class Generic {
	/**
	 * @param {string} name
	 * @param {string} doc
	 */
	constructor(name, doc = null) {
		this.name = name
		this.doc = doc
	}
}

class Constraint {
	/**
	 * @param {string} name
	 * @param {string} doc
	 * @param {Map<string, ConstraintParameter>} parameters
	 * @param {Constraint[]} aliases
	 * @param {Type[]} applicableTypes
	 */
	constructor(name, doc = null, parameters = new Map(), aliases = [], applicableTypes = []) {
		this.name = name
		this.doc = doc
		this.parameters = parameters
		this.aliases = aliases
		this.appliedTypes = applicableTypes
	}

	/**
	 * @param {Constraint} base
	 */
	rebaseOn(base) {
		this.name = this.name ?? base.name
		this.doc = this.doc == null || this.doc.length == 0 ? base.doc : this.doc

		for (const [ name, baseParam ] of base.parameters) {
			const thisParam = this.parameters.get(name)

			if (thisParam) thisParam.rebaseOn(baseParam)
			else this.parameters.set(name, baseParam)
		}
	}
}

class ConstraintParameter {
	/**
	 * @param {string} name
	 * @param {string} doc
	 * @param {string} type
	 * @param {string} defaultValue
	 */
	constructor(name, doc = null, type = null, defaultValue = null) {
		this.name = name
		this.doc = doc
		this.type = type
		this.defaultValue = defaultValue
	}

	/**
	 * @param {ConstraintParameter} base
	 */
	rebaseOn(base) {
		this.doc = this.doc == null || this.doc.length == 0 ? base.doc : this.doc
		this.type = this.type ?? base.type
		this.defaultValue = this.defaultValue ?? base.defaultValue
	}
}

class ConstraintParameterType {
	/**
	 * @param {string} name
	 * @param {string} doc
	 */
	constructor(name, doc) {
		this.name = name
		this.doc = doc
	}
}

const FromXml = {
	/**
	 * @param {xmldoc.XmlElement} root
	 */
	readConstraints(root) {
		return root.childrenNamed("constraint").map(xml => {
			const constraint = new Constraint(
				xml.attr.id,
				xml.childNamed("desc").val,
				FromXml.readConstraintParameters(xml).toMap(param => param.name),
			)

			FromXml.readConstraintAliases(xml, constraint)

			return constraint
		})
	},

	/**
	 * @param {xmldoc.XmlElement} constraint
	 */
	readConstraintParameters(constraint) {
		return constraint.childrenNamed("param").map(xml => new ConstraintParameter(
			xml.attr.name,
			xml.val,
			xml.attr.type,
			xml.attr.default
		))
	},

	/**
	 * @param {xmldoc.XmlElement} constraint
	 * @param {Constraint} base
	 */
	readConstraintAliases(constraint, base) {
		for (const xml of constraint.childrenNamed("alias")) {
			const alias = new Constraint(
				xml.attr.id,
				xml.childNamed("desc")?.val,
				FromXml.readConstraintParameters(xml).toMap(param => param.name),
			)

			alias.rebaseOn(base)

			base.aliases.push(alias)
		}
	},

	/**
	 * @param {xmldoc.XmlElement} root
	 * @param {Map<string, Constraint} constraints
	 */
	readTypes(root, constraints) {
		return root.childrenNamed("type").map(xml => {
			const type = new Type(
				xml.attr.id,
				xml.childNamed("desc").val,
				FromXml.readTypeGenerics(xml)
			)

			type.constraints = FromXml.readTypeConstraints(xml, constraints, type)

			return type
		})
	},
	/**
	 * @param {xmldoc.XmlElement} type
	 */
	readTypeGenerics(type) {
		return type.childrenNamed("generic").map(xml => new Generic(
			xml.attr.name,
			xml.val
		))
	},

	/**
	 *
	 * @param {xmldoc.XmlElement} type
	 * @param {Map<string, Constraint>} constraints
	 * @param {Type} applicableType
	 */
	readTypeConstraints(type, constraints, applicableType) {
		return type.childrenNamed("constraint").map(xml => {
			const typeConstraint = new Constraint(
				xml.attr.cref,
				xml.childNamed("desc")?.val,
				FromXml.readConstraintParameters(xml).toMap(parameter => parameter.name)
			)

			const baseConstraint = constraints.get(typeConstraint.name)

			baseConstraint.appliedTypes.push(applicableType)
			typeConstraint.rebaseOn(baseConstraint)

			return typeConstraint
		})
	},

	/**
	 * @param {xmldoc.XmlElement} root
	 */
	readConstraintParameterTypes(root) {
		return root.childrenNamed("cptype").map(xml => new ConstraintParameterType(
			xml.attr.id,
			xml.val
		))
	}
}

const ToHtml = {
	/**
	 * @param {Type} type
	 */
	writeType(type) {
		return `
			<!DOCTYPE html>
			<html lang="en">
				<head>
					<meta charset="utf-8"/>
					<title>${ type.name }</title>
				</head>
				<body style="background: black; color: white;">
					<h1>${ type.name }</h1>
					<p>${ type.doc }</p>
					${ ToHtml.writeGenerics(type.generics) }
					${ ToHtml.writeTypeConstraints(type.constraints) }
				</body>
			</html>
		`
	},

	/**
	 * @param {Generic[]} generics
	 */
	writeGenerics(generics) {
		if (generics.length) return `
			<h2>Generics:</h2>
			<ol>
				${ generics.map(ToHtml.writeGeneric).join("") }
			</ol>
		`

		else return ""
	},

	/**
	 * @param {Generic} generic
	 */
	writeGeneric(generic) {
		return `
			<li>
				<em> ${ generic.name }: </em>
				<span> ${ generic.doc } </span>
			</li>
		`
	},

	/**
	 * @param {Constraint[]} constraints
	 */
	writeTypeConstraints(constraints) {
		if (constraints.length) return `
			<h2>Constraints:</h2>
			<ul>
				${ constraints.map(ToHtml.writeTypeConstraint).join("") }
			</ul>
		`

		else return ""
	},

	/**
	 * @param {Constraint} constraint
	 */
	writeTypeConstraint(constraint) {
		return `
			<li>
				<a href="../constraints/${ constraint.name }.html"> ${ constraint.name }: </a>
				<span> ${ constraint.doc } </span>
				${ ToHtml.writeTypeConstraintParameters(Array.from(constraint.parameters.values())) }
			</li>
		`
	},

	/**
	 * @param {ConstraintParameter[]} parameters
	 */
	writeTypeConstraintParameters(parameters) {
		if (parameters.length) return `
			<h3>Parameters:</h3>
			<ul>
				${ parameters.map(ToHtml.writeTypeConstraintParameter).join("") }
			</ul>
		`

		else return ""
	},

	/**
	 * @param {ConstraintParameter} parameter
	 */
	writeTypeConstraintParameter(parameter) {
		return `
			<li>
				<em> ${ parameter.name }: </em>
				${ parameter.type ? `<span> (<a href="../constraints/ptypes/index.html#${ parameter.type }">${ parameter.type }</a>) </span>` : "" }
				<span> ${ parameter.doc } </span>
				${ parameter.defaultValue ? `<span> Defaults to ${ parameter.defaultValue }. </span>` : "" }
			</li>
		`
	},

	/**
	 * @param {Constraint} constraint
	 */
	writeConstraint(constraint) {
		return `
			<!DOCTYPE html>
			<html lang="en">
				<head>
					<meta charset="utf-8"/>
					<title>${ constraint.name }</title>
				</head>
				<body style="background: black; color: white;">
					<h1>${ constraint.name }</h1>
					<p>${ constraint.doc }</p>
					${ ToHtml.writeConstraintParameters(Array.from(constraint.parameters.values())) }
					${ ToHtml.writeConstraintAliases(constraint.aliases) }
					${ ToHtml.writeConstraintApplicableTypes(constraint.appliedTypes) }
				</body>
			</html>
		`
	},

	/**
	 * @param {ConstraintParameter[]} parameters
	 */
	writeConstraintParameters(parameters) {
		if (parameters.length) return `
			<h3>Parameters:</h3>
			<ul>
				${ parameters.map(ToHtml.writeConstraintParameter).join("") }
			</ul>
		`

		else return ""
	},

	/**
	 * @param {ConstraintParameter} parameter
	 */
	writeConstraintParameter(parameter) {
		return `
			<li>
				<em> ${ parameter.name }: </em>
				${ parameter.type ? `<span> (<a href="./ptypes/index.html#${ parameter.type }">${ parameter.type }</a>) </span>` : "" }
				<span> ${ parameter.doc } </span>
				${ parameter.defaultValue ? `<span> Defaults to ${ parameter.defaultValue }. </span>` : "" }
			</li>
		`
	},

	/**
	 * @param {Constraint[]} constraints
	 */
	writeConstraintAliases(constraints) {
		if (constraints.length) return `
			<h2>Aliases:</h2>
			<ul>
				${ constraints.map(ToHtml.writeConstraintAlias).join("") }
			</ul>
		`

		else return ""
	},

	/**
	 * @param {Constraint} constraint
	 */
	writeConstraintAlias(constraint) {
		return `
			<li>
				<em> ${ constraint.name }: </em>
				<span> ${ constraint.doc } </span>
				${ ToHtml.writeConstraintParameters(Array.from(constraint.parameters.values())) }
				${ ToHtml.writeConstraintAliases(constraint.aliases) }
			</li>
		`
	},

	/**
	 * @param {ConstraintParameter[]} parameters
	 */
	writeConstraintAliasParameters(parameters) {
		if (parameters.length) return `
			<h3>Parameters:</h3>
			<ul>
				${ parameters.map(ToHtml.writeConstraintAliasParameter).join("") }
			</ul>
		`

		else return ""
	},

	/**
	 * @param {ConstraintParameter} parameter
	 */
	writeConstraintAliasParameter(parameter) {
		return `
			<li>
				<em> ${ parameter.name }: </em>
				${ parameter.type ? `<span> (<a href="./ptypes/index.html#${ parameter.type }">${ parameter.type }</a>) </span>` : "" }
				<span> ${ parameter.doc } </span>
				${ parameter.defaultValue ? `<span> Defaults to ${ parameter.defaultValue }. </span>` : "" }
			</li>
		`
	},

	/**
	 * @param {Type[]} types
	 */
	writeConstraintApplicableTypes(types) {
		if (types.length) return `
			<h2>Applies to:</h2>
			<ul>
				${ types.map(ToHtml.writeConstraintApplicableType).join("") }
			</ul>
		`

		else return ""
	},

	/**
	 * @param {Type} type
	 */
	writeConstraintApplicableType(type) {
		return `
			<li>
				<a href="../types/${ type.name }.html"> ${ type.name } </a>
			</li>
		`
	},

	/**
	 * @param {ConstraintParameterType[]} types
	 */
	writeConstraintParameterTypes(types) {
		return `
			<!DOCTYPE html>
			<html lang="en">
				<head>
					<meta charset="utf-8"/>
					<title>Constraint Parameter Types</title>
				</head>
				<body style="background: black; color: white;">
					<h1>Constraint Parameter Types</h1>
					<ul>
						${ types.map(ToHtml.writeConstraintParameterType).join("") }
					</ul>
				</body>
			</html>
		`
	},

	/**
	 * @param {ConstraintParameterType} type
	 */
	writeConstraintParameterType(type) {
		return `
			<li id="${ type.name }">
				<em> ${ type.name }: </em>
				<span> ${ type.doc } </span>
			</li>
		`
	}
}

const xml = fs.readFileSync("./types-and-constraints.xml", "utf-8")
const nap = new xmldoc.XmlDocument("<nap>" + xml + "</nap>")

const constraints = FromXml.readConstraints(nap)
const types = FromXml.readTypes(nap, constraints.toMap(constraint => constraint.name))
const constraintParameterTypes = FromXml.readConstraintParameterTypes(nap)

fs.rmdirSync("./out", { recursive: true })
fs.mkdirSync("./out/types", { recursive: true })
fs.mkdirSync("./out/constraints/ptypes", { recursive: true })

for (const type of types) {
	const fileName = type.name + ".html"
	const fileContent = ToHtml.writeType(type)

	fs.writeFile("./out/types/" + fileName, fileContent, error => { if (error) throw error })
}

for (const constraint of constraints) {
	const fileName = constraint.name + ".html"
	const fileContent = ToHtml.writeConstraint(constraint)

	fs.writeFile("./out/constraints/" + fileName, fileContent, error => { if (error) throw error })
}

const fileName = "index.html"
const fileContent = ToHtml.writeConstraintParameterTypes(constraintParameterTypes)

fs.writeFile("./out/constraints/ptypes/" + fileName, fileContent, error => { if (error) throw error })
