# Contrato da API – PortalTarefas

## Recurso
Tarefas

## Rotas e métodos HTTP

| Método | URI | Resultado esperado |
|---|---|---|
| GET | /api/tarefas | Listar tarefas com paginação e filtros |
| GET | /api/tarefas/{id} | Consultar uma tarefa |
| POST | /api/tarefas | Cadastrar uma tarefa |
| PUT | /api/tarefas/{id} | Atualizar uma tarefa |
| DELETE | /api/tarefas/{id} | Excluir uma tarefa |

## Códigos HTTP

- 200: consulta ou atualização realizada.
- 201: tarefa criada, com cabeçalho Location.
- 204: tarefa excluída.
- 400: parâmetros ou dados inválidos.
- 404: tarefa não encontrada.
- 409: conflito de versão durante a atualização.

## Parâmetros da listagem

- pagina: número da página, padrão 1 e mínimo 1.
- tamanho: quantidade de itens, padrão 10, mínimo 1 e máximo 50.
- ordenarPor: id, titulo, situacao ou versao.
- direcao: asc ou desc.
- situacao: filtro pela situação da tarefa.
- termo: pesquisa por título.

## Regras

A API utilizará DTOs próprios para entrada e saída, sem expor
diretamente a entidade persistida.

A paginação, a ordenação e os filtros serão aplicados no banco
de dados antes da consulta ser materializada.

A ordenação aceitará somente os campos permitidos.

Os erros serão apresentados no formato Problem Details.
