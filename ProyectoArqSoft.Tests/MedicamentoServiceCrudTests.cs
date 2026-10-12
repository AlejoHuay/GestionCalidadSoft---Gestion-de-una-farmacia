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
