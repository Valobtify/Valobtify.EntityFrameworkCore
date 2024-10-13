using Resulver;

namespace Valobtify.EntityFrameworkCore;

sealed class Template : SingleValueObject<Template, string>, ICreatableValueObject<Template, string>
{
    private Template(string value) : base(value) { }
    public static Result<Template> Create(string value) => new Template(value);
}
