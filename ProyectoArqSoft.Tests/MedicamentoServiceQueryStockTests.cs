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

    [Fact]
    public void UpdateStock_returns_not_found_without_validating_or_updating()
    {
        var repository = StubProxy<IMedicamentoRepository>.Create((method, _) =>
            method.Name == nameof(IMedicamentoRepository.GetById)
                ? null
                : Unexpected(method));
        var service = CreateService(repository);

        var result = service.UpdateStock(14, 5, true, 29);

        Assert.False(result.IsSuccess);
        Assert.Equal("El medicamento no existe o no está activo.", result.Error);
    }

    [Fact]
    public void UpdateStock_propagates_validation_failure_without_updating()
    {
        var medicine = new Medicamento { Id = 14, Stock = 3 };
        var repository = StubProxy<IMedicamentoRepository>.Create((method, arguments) =>
            method.Name == nameof(IMedicamentoRepository.GetById) &&
            Equals(arguments[0], 14)
                ? medicine
                : Unexpected(method));
        var service = CreateService(repository, new MovimientoStockValidacion());

        var result = service.UpdateStock(14, 5, false, 29);

        Assert.False(result.IsSuccess);
        Assert.Equal("Stock insuficiente para realizar la venta.", result.Error);
    }

    [Theory]
    [InlineData(true, 1, true)]
    [InlineData(false, 1, true)]
    [InlineData(true, 0, false)]
    public void UpdateStock_validates_movement_and_updates_repository(
        bool isEntry,
        int affectedRows,
        bool expectedSuccess)
    {
        MovimientoStockDto? validatedMovement = null;
        (int Id, int Quantity, bool IsEntry, int UserId)? repositoryUpdate = null;
        var medicine = new Medicamento { Id = 14, Stock = 20 };
        var repository = StubProxy<IMedicamentoRepository>.Create((method, arguments) =>
        {
            if (method.Name == nameof(IMedicamentoRepository.GetById) &&
                Equals(arguments[0], 14))
            {
                return medicine;
            }

            if (method.Name == nameof(IMedicamentoRepository.UpdateStock))
            {
                repositoryUpdate = (
                    (int)arguments[0]!,
                    (int)arguments[1]!,
                    (bool)arguments[2]!,
                    (int)arguments[3]!);
                return affectedRows;
            }

            return Unexpected(method);
        });
        var movementValidator = StubProxy<IResult<MovimientoStockDto>>.Create((method, arguments) =>
        {
            Assert.Equal(nameof(IResult<MovimientoStockDto>.Validar), method.Name);
            validatedMovement = Assert.IsType<MovimientoStockDto>(arguments[0]);
            return Result.Ok();
        });
        var service = CreateService(repository, movementValidator);

        var result = service.UpdateStock(14, 5, isEntry, 29);

        Assert.Equal(expectedSuccess, result.IsSuccess);
        Assert.NotNull(validatedMovement);
        Assert.Equal(14, validatedMovement.IdMedicamento);
        Assert.Equal(5, validatedMovement.Cantidad);
        Assert.Equal(isEntry, validatedMovement.EsEntrada);
        Assert.Equal(29, validatedMovement.IdUsuario);
        Assert.Equal(20, validatedMovement.StockActual);
        Assert.Equal((14, 5, isEntry, 29), repositoryUpdate);
        if (!expectedSuccess)
            Assert.Equal("No se pudo actualizar el stock del medicamento.", result.Error);
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
