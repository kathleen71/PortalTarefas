import { TarefasClient } from "./TarefasClient";

const cliente = new TarefasClient("http://localhost:5082", {
    fetch: (url, init) => fetch(url, init)
});

async function testar() {
    const resultado = await cliente.listarTarefas(
        1, 10, "id", "asc", undefined, undefined
    );

    console.log("Consulta realizada pelo cliente TypeScript:");
    console.log(JSON.stringify(resultado, null, 2));
}

testar().catch(erro => {
    console.error("Erro ao consultar a API:", erro);
});
