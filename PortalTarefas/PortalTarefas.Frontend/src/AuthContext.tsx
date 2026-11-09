import { createContext, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import {
  consultarSessao,
  fazerLogin,
  fazerLogout,
  type Usuario,
} from './api/auth'

interface AuthContexto {
  usuario: Usuario | null
  carregando: boolean
  entrar: (email: string, senha: string) => Promise<void>
  sair: () => Promise<void>
}

const Contexto = createContext<AuthContexto | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<Usuario | null>(null)
  const [carregando, setCarregando] = useState(true)

  useEffect(() => {
    consultarSessao()
      .then(setUsuario)
      .catch(() => setUsuario({ autenticado: false }))
      .finally(() => setCarregando(false))
  }, [])

  async function entrar(email: string, senha: string) {
    const dados = await fazerLogin(email, senha)
    setUsuario(dados)
  }

  async function sair() {
    await fazerLogout()
    setUsuario({ autenticado: false })
  }

  return (
    <Contexto.Provider value={{ usuario, carregando, entrar, sair }}>
      {children}
    </Contexto.Provider>
  )
}

// O hook compartilha o contexto com o AuthProvider neste arquivo.
// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const contexto = useContext(Contexto)
  if (!contexto) throw new Error('AuthProvider não encontrado.')
  return contexto
}
