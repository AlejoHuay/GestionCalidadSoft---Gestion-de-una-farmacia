using System.Data;
using System.Reflection;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Application.Services;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Domain.Validators;

namespace ProyectoArqSoft.Tests;

public class MedicamentoServiceQueryStockTests
{
    [Fact]
    public void Query_methods_return_repository_results_and_forward_filter_and_id()
    {
        var all = new DataTable();
        var filtered = new DataTable();
        var featured = new DataTable();
        var expectedMedicine = new Medicamento { Id = 14 };
        var calls = new List<(string Method, object?[] Arguments)>();
        var repository = StubProxy<IMedicamentoRepository>.Create((method, arguments) =>
        {
            calls.Add((method.Name, arguments));
            return method.Name switch
            {
                nameof(IMedicamentoRepository.GetAll) when arguments.Length == 0 => all,
                nameof(IMedicamentoRepository.GetAll) => filtered,
                nameof(IMedicamentoRepository.GetById) => expectedMedicine,
                nameof(IMedicamentoRepository.GetDestacados) => featured,
                _ => Unexpected(method)
            };
        });
        var service = CreateService(repository);

        Assert.Same(all, service.ObtenerTodos());
        Assert.Same(filtered, service.ObtenerTodos("ibuprofeno"));
        Assert.Same(expectedMedicine, service.ObtenerPorId(14));
        Assert.Same(featured, service.ObtenerDestacados());

        Assert.Collection(
            calls,
            call => AssertCall(call, nameof(IMedicamentoRepository.GetAll)),
            call => AssertCall(call, nameof(IMedicamentoRepository.GetAll), "ibuprofeno"),
            call => AssertCall(call, nameof(IMedicamentoRepository.GetById), 14),
            call => AssertCall(call, nameof(IMedicamentoRepository.GetDestacados)));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void EliminarLogicamente_returns_result_based_on_repository_rows(
        int affectedRows,
        bool expectedSuccess)
    {
        Medicamento? deleted = null;
        var repository = StubProxy<IMedicamentoRepository>.Create((method, arguments) =>
        {
            if (method.Name == nameof(IMedicamentoRepository.Delete))
            {
                deleted = Assert.IsType<Medicamento>(arguments[0]);
                return affectedRows;
            }

            return Unexpected(method);
        });
        var service = CreateService(repository);

        var result = service.EliminarLogicamente(14, 29);

        Assert.Equal(expectedSuccess, result.IsSuccess);
        Assert.NotNull(deleted);
        Assert.Equal(14, deleted.Id);
        Assert.Equal(29, deleted.IdUsuario);
        if (!expectedSuccess)
            Assert.Equal("No se pudo eliminar el medicamento.", result.Error);
    }

    private static MedicamentoService CreateService(
        IMedicamentoRepository repository,
        IResult<MovimientoStockDto>? movementValidator = null) =>
        new(
            repository,
            StubProxy<IResult<Medicamento>>.Create((method, _) => Unexpected(method)),
            movementValidator ?? StubProxy<IResult<MovimientoStockDto>>.Create(
                (method, _) => Unexpected(method)));

    private static void AssertCall(
        (string Method, object?[] Arguments) call,
        string method,
        params object?[] arguments)
    {
        Assert.Equal(method, call.Method);
        Assert.Equal(arguments, call.Arguments);
    }

    private static object Unexpected(MethodInfo method) =>
        throw new InvalidOperationException($"Unexpected service call: {method.Name}");
}
