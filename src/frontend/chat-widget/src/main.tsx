import React from "react";
import ReactDOM from "react-dom/client";
import { ChatWidget } from "./components/ChatWidget";
import "./styles.css";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || "http://localhost:5000";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <ChatWidget apiBaseUrl={apiBaseUrl} />
  </React.StrictMode>,
);
