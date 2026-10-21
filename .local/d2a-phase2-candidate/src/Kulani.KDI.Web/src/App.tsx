import { useEffect, useState } from 'react';
import { checkConnection, type ConnectionState } from './features/foundation/connection';

const labels = {
  loading: 'Comprobando conexión…',
  connected: 'API conectada',
  unavailable: 'API no disponible',
};

export function App() {
  const [connection, setConnection] = useState<ConnectionState>({ kind: 'loading' });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    let active = true;
    const timeout = window.setTimeout(() => controller.abort(), 5000);
    checkConnection(controller.signal)
      .then(() => { if (active) setConnection({ kind: 'connected' }); })
      .catch(() => { if (active) setConnection({ kind: 'unavailable' }); })
      .finally(() => window.clearTimeout(timeout));
    return () => {
      active = false;
      window.clearTimeout(timeout);
      controller.abort();
    };
  }, [attempt]);

  function retry() {
    setConnection({ kind: 'loading' });
    setAttempt(value => value + 1);
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="#main" aria-label="KDI, inicio"><span className="brand-mark">K</span><span>KDI</span></a>
        <span className="product-name">Kulani Delivery Intelligence</span>
        <span className="environment">Fundación</span>
      </header>
      <main id="main">
        <p className="eyebrow">UN ESPACIO PARA CONECTAR EL TRABAJO</p>
        <h1>De la intención<br />a la entrega.</h1>
        <p className="intro">El punto de partida para dar seguimiento a la demanda, el trabajo y los resultados de tu organización.</p>
        <section className="connection-card" aria-labelledby="connection-title">
          <div className="card-heading"><span className={`status-dot ${connection.kind}`} aria-hidden="true" /><h2 id="connection-title">Conexión del servicio</h2></div>
          <p role="status" aria-live="polite">{labels[connection.kind]}</p>
          <p className="supporting">{connection.kind === 'connected'
            ? 'El servicio responde. Las funciones de tu organización estarán disponibles en una próxima etapa.'
            : 'Puedes volver a comprobar la conexión cuando lo necesites.'}</p>
          <button type="button" onClick={retry} disabled={connection.kind === 'loading'}>Volver a comprobar <span aria-hidden="true">↗</span></button>
        </section>
        <div className="journey" aria-label="Visión del producto"><span>Demanda</span><span aria-hidden="true">→</span><span>Trabajo</span><span aria-hidden="true">→</span><span>Entrega</span></div>
      </main>
      <footer><span>Kulani Delivery Intelligence</span><span>Construyendo la base.</span></footer>
    </div>
  );
}
