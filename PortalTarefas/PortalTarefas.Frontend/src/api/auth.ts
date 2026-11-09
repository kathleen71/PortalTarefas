const API_URL = import.meta.env.VITE_API_URL || '/api'

export interface Usuario {
  autenticado: boolean
  email?: string
  roles?: string[]
  permissoes?: string[]
}

let tokenCsrf = ''

export async function obterCsrf() {
  const resposta = await fetch(`${API_URL}/auth/csrf`, {
    credentials: 'include',
  })

  if (!resposta.ok) throw new Error('Não foi possível obter o token de segurança.')

  const dados = await resposta.json()
  tokenCsrf = dados.token
  return tokenCsrf
}

export async function consultarSessao(): Promise<Usuario> {
  const resposta = await fetch(`${API_URL}/auth/me`, {
    credentials: 'include',
  })

  if (!resposta.ok) throw new Error('Falha ao consultar sessão.')
  return resposta.json()
}

export async function fazerLogin(email: string, senha: string) {
  const csrf = await obterCsrf()

  const resposta = await fetch(`${API_URL}/auth/login`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF-TOKEN': csrf,
    },
    body: JSON.stringify({ email, senha }),
  })

  if (!resposta.ok) throw new Error('E-mail ou senha inválidos.')
  return consultarSessao()
}

export async function fazerLogout() {
  const csrf = await obterCsrf()

  const resposta = await fetch(`${API_URL}/auth/logout`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'X-CSRF-TOKEN': csrf,
    },
  })

  if (!resposta.ok) throw new Error('Não foi possível sair da conta.')
  tokenCsrf = ''
}
