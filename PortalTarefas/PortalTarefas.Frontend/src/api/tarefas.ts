const API_URL = import.meta.env.VITE_API_URL || '/api'
let tokenCsrf = ''

async function obterCsrf() {
  const resposta = await fetch(`${API_URL}/auth/csrf`, { credentials: 'include' })
  if (!resposta.ok) throw new Error('Falha ao obter token CSRF.')
  const dados = await resposta.json()
  tokenCsrf = dados.token
  return tokenCsrf
}

export interface TarefaResumo {
  id: number
  titulo: string
  situacao: string
  versao: number
}

export interface TarefaDetalhe extends TarefaResumo {
  descricao: string
}

export interface Pagina<T> {
  itens: T[]
  pagina: number
  tamanho: number
  total: number
  totalPaginas: number
}

export interface FiltrosTarefa {
  pagina?: number
  tamanho?: number
  ordenarPor?: string
  direcao?: string
  situacao?: string
  termo?: string
}

export interface NovaTarefa {
  titulo: string
  descricao: string
  situacao: string
}

export interface ErroApi {
  mensagem: string
  status: number
  titulo?: string
  campos?: Record<string, string[]>
}

async function requisicao<T>(
  caminho: string,
  opcoes: RequestInit = {},
): Promise<T> {
  let resposta: Response

  try {
    const metodo = (opcoes.method || 'GET').toUpperCase()
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...Object.fromEntries(new Headers(opcoes.headers).entries()),
    }
    if (['POST', 'PUT', 'DELETE'].includes(metodo)) {
      headers['X-CSRF-TOKEN'] = await obterCsrf()
    }
    resposta = await fetch(`${API_URL}${caminho}`, {
      ...opcoes,
      credentials: 'include',
      headers,
    })
  } catch (erro) {
    if (erro instanceof DOMException && erro.name === 'AbortError') {
      throw erro
    }

    throw {
      mensagem: 'Não foi possível conectar à API.',
      status: 0,
    } satisfies ErroApi
  }

  if (!resposta.ok) {
    const conteudo = await resposta.json().catch(() => null)

    throw {
      mensagem:
        conteudo?.detail ||
        conteudo?.title ||
        `Erro na requisição: ${resposta.status}`,
      status: resposta.status,
      titulo: conteudo?.title,
      campos: conteudo?.errors,
    } satisfies ErroApi
  }

  if (resposta.status === 204) {
    return undefined as T
  }

  return resposta.json() as Promise<T>
}

export function listarTarefas(
  filtros: FiltrosTarefa = {},
  sinal?: AbortSignal,
) {
  const parametros = new URLSearchParams()

  Object.entries(filtros).forEach(([chave, valor]) => {
    if (valor !== undefined && valor !== '') {
      parametros.set(chave, String(valor))
    }
  })

  const consulta = parametros.toString()

  return requisicao<Pagina<TarefaResumo>>(
    `/tarefas/${consulta ? `?${consulta}` : ''}`,
    { signal: sinal },
  )
}

export function buscarTarefa(id: number, sinal?: AbortSignal) {
  return requisicao<TarefaDetalhe>(`/tarefas/${id}`, {
    signal: sinal,
  })
}

export function criarTarefa(tarefa: NovaTarefa) {
  return requisicao<TarefaDetalhe>('/tarefas/', {
    method: 'POST',
    body: JSON.stringify(tarefa),
  })
}
