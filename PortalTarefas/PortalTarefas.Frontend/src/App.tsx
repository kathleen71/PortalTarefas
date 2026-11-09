import { useEffect, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import {
  BrowserRouter, Routes, Route, Link, Navigate,
  useNavigate, useParams, useSearchParams, useLocation,
} from 'react-router-dom'
import { AuthProvider, useAuth } from './AuthContext'
import {
  listarTarefas, buscarTarefa, criarTarefa,
  type TarefaResumo, type TarefaDetalhe, type ErroApi,
} from './api/tarefas'
import './App.css'

function mensagemErro(erro: unknown) {
  return (erro as ErroApi)?.mensagem || 'Ocorreu um erro inesperado.'
}

function RotaProtegida({ children }: { children: ReactNode }) {
  const { usuario, carregando } = useAuth()
  const local = useLocation()

  if (carregando) return <p>Verificando sessão...</p>

  if (!usuario?.autenticado) {
    return <Navigate to="/login" state={{ from: local }} replace />
  }

  return <>{children}</>
}

function Layout({ children }: { children: ReactNode }) {
  const { usuario, sair } = useAuth()
  const navegar = useNavigate()

  async function logout() {
    try {
      await sair()
      navegar('/login')
    } catch {
      alert('Não foi possível encerrar a sessão.')
    }
  }

  return (
    <div className="container">
      <header>
        <div>
          <h1>Portal de Tarefas</h1>
          <p>Organize suas atividades em um só lugar.</p>
          <small>
            {usuario?.email} · {usuario?.roles?.join(', ')}
          </small>
        </div>
        <nav>
          <Link to="/">Início</Link>
          <Link to="/nova">Nova tarefa</Link>
          <button className="secundario" onClick={logout}>Sair</button>
        </nav>
      </header>
      <main>{children}</main>
      <footer>Portal de Tarefas · React + TypeScript</footer>
    </div>
  )
}

function Login() {
  const { entrar, usuario, carregando } = useAuth()
  const navegar = useNavigate()
  const local = useLocation()
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState('')
  const [enviando, setEnviando] = useState(false)

  useEffect(() => {
    if (!carregando && usuario?.autenticado) navegar('/', { replace: true })
  }, [carregando, usuario, navegar])

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setErro('')
    setEnviando(true)

    try {
      await entrar(email, senha)
      const destino = local.state?.from?.pathname || '/'
      navegar(destino, { replace: true })
    } catch {
      setErro('E-mail ou senha inválidos. Confira os dados e tente novamente.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="container">
      <main>
        <section className="painel">
          <h1>Portal de Tarefas</h1>
          <h2>Entrar no sistema</h2>
          <form onSubmit={enviar}>
            <label>E-mail
              <input type="email" required value={email}
                onChange={e => setEmail(e.target.value)} />
            </label>
            <label>Senha
              <input type="password" required value={senha}
                onChange={e => setSenha(e.target.value)} />
            </label>
            {erro && <div className="erro" role="alert">{erro}</div>}
            <button className="botao" disabled={enviando}>
              {enviando ? 'Entrando...' : 'Entrar'}
            </button>
          </form>
        </section>
      </main>
    </div>
  )
}

function ListaTarefas() {
  const [params, setParams] = useSearchParams()
  const [tarefas, setTarefas] = useState<TarefaResumo[]>([])
  const [total, setTotal] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(0)
  const [estado, setEstado] = useState('loading')
  const [erro, setErro] = useState('')

  const pagina = Number(params.get('pagina') || 1)
  const situacao = params.get('situacao') || ''
  const termo = params.get('termo') || ''
  const ordenarPor = params.get('ordenarPor') || 'id'
  const direcao = params.get('direcao') || 'asc'
  const simulacao = params.get('simular') || ''

  useEffect(() => {
    const controle = new AbortController()
    let ativo = true

    async function carregar() {
      setEstado('loading')
      setErro('')
      try {
        if (simulacao === 'erro') {
          throw { mensagem: 'Falha simulada para testar o estado de erro.' }
        }
        if (simulacao === 'demora') {
          await new Promise(resolve => setTimeout(resolve, 1500))
        }

        const resultado = await listarTarefas({
          pagina, tamanho: 5, situacao, termo, ordenarPor, direcao,
        }, controle.signal)

        if (!ativo) return
        setTarefas(resultado.itens)
        setTotal(resultado.total)
        setTotalPaginas(resultado.totalPaginas)
        setEstado(resultado.itens.length ? 'success' : 'empty')
      } catch (e) {
        if (!ativo || (e instanceof DOMException && e.name === 'AbortError')) return
        setErro(mensagemErro(e))
        setEstado('error')
      }
    }

    carregar()
    return () => {
      ativo = false
      controle.abort()
    }
  }, [pagina, situacao, termo, ordenarPor, direcao, simulacao])

  function atualizar(chave: string, valor: string) {
    const novos = new URLSearchParams(params)
    if (valor) novos.set(chave, valor)
    else novos.delete(chave)
    novos.set('pagina', '1')
    setParams(novos)
  }

  return (
    <section>
      <div className="titulo-linha">
        <div>
          <h2>Minhas tarefas</h2>
          <p className="subtitulo">{total} tarefa(s) cadastrada(s)</p>
        </div>
        <Link className="botao" to="/nova">+ Nova tarefa</Link>
      </div>

      <div className="filtros">
        <input placeholder="Buscar pelo título..." value={termo}
          onChange={e => atualizar('termo', e.target.value)} />
        <select value={situacao} onChange={e => atualizar('situacao', e.target.value)}>
          <option value="">Todas as situações</option>
          <option>Pendente</option>
          <option>Em andamento</option>
          <option>Concluída</option>
        </select>
        <select value={ordenarPor} onChange={e => atualizar('ordenarPor', e.target.value)}>
          <option value="id">ID</option>
          <option value="titulo">Título</option>
          <option value="situacao">Situação</option>
          <option value="versao">Versão</option>
        </select>
        <select value={direcao} onChange={e => atualizar('direcao', e.target.value)}>
          <option value="asc">Crescente</option>
          <option value="desc">Decrescente</option>
        </select>
      </div>

      <div className="testes">
        <button className="secundario" onClick={() => atualizar('simular', 'demora')}>Testar demora</button>
        <button className="secundario" onClick={() => atualizar('simular', 'erro')}>Testar erro</button>
        <button className="secundario" onClick={() => atualizar('simular', '')}>Voltar ao normal</button>
      </div>

      <div aria-live="polite" className="resultado">
        {estado === 'loading' && <p>Carregando tarefas...</p>}
        {estado === 'error' && <div className="erro"><strong>Não foi possível carregar.</strong><p>{erro}</p></div>}
        {estado === 'empty' && <p>Nenhuma tarefa encontrada.</p>}
        {estado === 'success' && (
          <div className="lista">
            {tarefas.map(tarefa => (
              <article className="tarefa" key={tarefa.id}>
                <div>
                  <span className="id">#{tarefa.id}</span>
                  <h3>{tarefa.titulo}</h3>
                  <span className="situacao">{tarefa.situacao}</span>
                </div>
                <Link to={`/tarefas/${tarefa.id}`} className="link">Ver detalhes →</Link>
              </article>
            ))}
          </div>
        )}
      </div>

      <div className="paginacao">
        <button disabled={pagina <= 1} onClick={() => atualizar('pagina', String(pagina - 1))}>Anterior</button>
        <span>Página {pagina} de {totalPaginas || 1}</span>
        <button disabled={pagina >= totalPaginas} onClick={() => atualizar('pagina', String(pagina + 1))}>Próxima</button>
      </div>
    </section>
  )
}

function DetalhesTarefa() {
  const { id } = useParams()
  const [tarefa, setTarefa] = useState<TarefaDetalhe | null>(null)
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(true)

  useEffect(() => {
    const controle = new AbortController()
    buscarTarefa(Number(id), controle.signal)
      .then(setTarefa)
      .catch((e: unknown) => {
        if (!(e instanceof DOMException && e.name === 'AbortError')) setErro(mensagemErro(e))
      })
      .finally(() => setCarregando(false))
    return () => controle.abort()
  }, [id])

  if (carregando) return <p>Carregando detalhes...</p>
  if (erro) return <div className="erro"><h2>Não foi possível abrir a tarefa</h2><p>{erro}</p><Link to="/">Voltar</Link></div>
  if (!tarefa) return <p>Tarefa não encontrada.</p>

  return (
    <section className="painel">
      <h2>Detalhes da tarefa #{tarefa.id}</h2>
      <h3>{tarefa.titulo}</h3>
      <p>{tarefa.descricao || 'Sem descrição.'}</p>
      <p><strong>Situação:</strong> {tarefa.situacao}</p>
      <p><strong>Versão:</strong> {tarefa.versao}</p>
      <Link className="botao" to="/">Voltar para a lista</Link>
    </section>
  )
}

function NovaTarefa() {
  const navegar = useNavigate()
  const [titulo, setTitulo] = useState('')
  const [descricao, setDescricao] = useState('')
  const [situacao, setSituacao] = useState('Pendente')
  const [erro, setErro] = useState('')
  const [campos, setCampos] = useState<Record<string, string[]>>({})
  const [salvando, setSalvando] = useState(false)

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setErro('')
    setCampos({})
    setSalvando(true)
    try {
      const criada = await criarTarefa({ titulo, descricao, situacao })
      navegar(`/tarefas/${criada.id}`)
    } catch (e) {
      const falha = e as ErroApi
      setErro(mensagemErro(e))
      setCampos(falha.campos || {})
    } finally {
      setSalvando(false)
    }
  }

  return (
    <section className="painel">
      <h2>Cadastrar tarefa</h2>
      <form onSubmit={enviar}>
        <label>Título
          <input required maxLength={80} value={titulo} onChange={e => setTitulo(e.target.value)} />
        </label>
        {campos.Titulo && <small className="erro-texto">{campos.Titulo.join(', ')}</small>}
        <label>Descrição
          <textarea maxLength={300} value={descricao} onChange={e => setDescricao(e.target.value)} />
        </label>
        <label>Situação
          <select value={situacao} onChange={e => setSituacao(e.target.value)}>
            <option>Pendente</option>
            <option>Em andamento</option>
            <option>Concluída</option>
          </select>
        </label>
        {erro && <div className="erro" role="alert">{erro}</div>}
        <div className="acoes">
          <button className="botao" disabled={salvando}>{salvando ? 'Salvando...' : 'Salvar tarefa'}</button>
          <Link to="/" className="link">Cancelar</Link>
        </div>
      </form>
    </section>
  )
}

function Aplicacao() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/" element={<RotaProtegida><Layout><ListaTarefas /></Layout></RotaProtegida>} />
          <Route path="/tarefas/:id" element={<RotaProtegida><Layout><DetalhesTarefa /></Layout></RotaProtegida>} />
          <Route path="/nova" element={<RotaProtegida><Layout><NovaTarefa /></Layout></RotaProtegida>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default Aplicacao
