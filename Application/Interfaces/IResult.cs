using ProyectoArqSoft.Domain.Validators;

namespace ProyectoArqSoft.Application.Interfaces
{
    public interface IResult<in T>
    {
        Result Validar(T entidad);
    }
}
