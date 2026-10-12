using System.Reflection;
using ProyectoArqSoft.Application.Interfaces;
using ProyectoArqSoft.Application.Ports.Output;
using ProyectoArqSoft.Application.Services;
using ProyectoArqSoft.Domain.DTOs;
using ProyectoArqSoft.Domain.Models;
using ProyectoArqSoft.Domain.Validators;

namespace ProyectoArqSoft.Tests;

public class MedicamentoServiceCrudTests
{
    [Fact]
    public void Crear_trims_text_and_inserts_medicine_with_user()
    {
        Medicamento? inserted = null;
        var repository = StubProxy<IMedicamentoRepository>.Create((method, arguments) =>
        {
            if (method.Name == nameof(IMedicamentoRepository.Insert))
            {
                inserted = Assert.IsType<Medicamento>(arguments[0]);
                return 1;
            }

            return Unexpected(method);
        });
        var service = CreateService(repository, new MedicamentoValidacion());

        var result = service.Crear(
            "  Paracetamol   500  ",
            " Tabletas ",
            4,
            " 500 mg ",
            2.5m,
            20,
            31);

        Assert.True(result.IsSuccess);
        Assert.NotNull(inserted);
        Assert.Equal(0, inserted.Id);
        Assert.Equal("Paracetamol 500", inserted.Nombre);
        Assert.Equal("Tabletas", inserted.Presentacion);
        Assert.Equal(4, inserted.IdClasificacion);
        Assert.Equal("500 mg", inserted.Concentracion);
        Assert.Equal(2.5m, inserted.Precio);
        Assert.Equal(20, inserted.Stock);
        Assert.Equal(31, inserted.IdUsuario);
    }

    [Fact]
    public void Crear_returns_validation_error_without_inserting()
    {
        var repository = StubProxy<IMedicamentoRepository>.Create((method, _) =>
            Unexpected(method));
        var service = CreateService(repository, new MedicamentoValidacion());

        var result = service.Crear("", "Tabletas", 4, "500 mg", 2.5m, 20, 31);

        Assert.False(result.IsSuccess);
        Assert.Equal("El nombre es obligatorio.", result.Error);
    }

    [Fact]
    public void Crear_returns_failure_when_repository_does_not_insert()
    {
        var repository = StubProxy<IMedicamentoRepository>.Create((method, _) =>
            method.Name == nameof(IMedicamentoRepository.Insert) ? 0 : Unexpected(method));
        var service = CreateService(repository, new MedicamentoValidacion());

        var result = service.Crear("Paracetamol", "Tabletas", 4, "500 mg", 2.5m, 20, 31);

        Assert.False(result.IsSuccess);
        Assert.Equal("No se pudo registrar el medicamento.", result.Error);
    }

    [Fact]
    public void Actualizar_maps_dto_and_returns_success_when_repository_updates()
    {
        Medicamento? updated = null;
        var repository = StubProxy<IMedicamentoRepository>.Create((method, arguments) =>
        {
            if (method.Name == nameof(IMedicamentoRepository.Update))
            {
                updated = Assert.IsType<Medicamento>(arguments[0]);
                return 1;
            }

            return Unexpected(method);
        });
        var service = CreateService(repository, new MedicamentoValidacion());

        var result = service.Actualizar(new MedicamentoActualizarDto
        {
            Id = 17,
            Nombre = "  Ibuprofeno  ",
            Presentacion = " Cápsulas ",
            IdClasificacion = 3,
            Concentracion = " 200 mg ",
            Precio = 6.75m,
            Stock = 25,
            IdUsuario = 42
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(updated);
        Assert.Equal(17, updated.Id);
        Assert.Equal("Ibuprofeno", updated.Nombre);
        Assert.Equal("Cápsulas", updated.Presentacion);
        Assert.Equal(3, updated.IdClasificacion);
        Assert.Equal("200 mg", updated.Concentracion);
        Assert.Equal(6.75m, updated.Precio);
        Assert.Equal(25, updated.Stock);
        Assert.Equal(42, updated.IdUsuario);
    }

    [Fact]
    public void Actualizar_returns_validation_error_without_updating()
    {
        var repository = StubProxy<IMedicamentoRepository>.Create((method, _) =>
            Unexpected(method));
        var service = CreateService(repository, new MedicamentoValidacion());

        var result = service.Actualizar(new MedicamentoActualizarDto
        {
            Id = 17,
            Nombre = "",
            Presentacion = "Cápsulas",
            IdClasificacion = 3,
            Concentracion = "200 mg",
            Precio = 6.75m,
            Stock = 25,
            IdUsuario = 42
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("El nombre es obligatorio.", result.Error);
    }

    private static MedicamentoService CreateService(
        IMedicamentoRepository repository,
        IResult<Medicamento> medicineValidator) =>
        new(
            repository,
            medicineValidator,
            StubProxy<IResult<MovimientoStockDto>>.Create((method, _) => Unexpected(method)));

    private static object Unexpected(MethodInfo method) =>
        throw new InvalidOperationException($"Unexpected service call: {method.Name}");
}
